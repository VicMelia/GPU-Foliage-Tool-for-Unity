using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSphereController : MonoBehaviour
{
    bool actived;
    
    //Components
    PlayerControls _playerControls;
    PlayerManager _playerManager;
    TargetLock _targetLockManager;
    Rigidbody _rb;
    SphereCollider _sphereCollider;
    CapsuleCollider _capsuleCollider;

    bool _isJumping;
    bool _isGrounded;
    float _speed = 25f;
    float _turnSpeed;
    Vector3 _moveInput;

    //Movement speed
    float _currentSpeedMultiplier = 1f;
    float _speedRecoveryRate = 2f;
    public Vector3 previousMoveDirection = Vector3.zero;
    bool _justFlipped = false;
    public LayerMask _groundMask;
    Vector3 _groundNormal = Vector3.up;

    //Life
    float _lifeSecondTimer = 15f;
    //Morphing
    bool _isMorphing;
    GameObject _closestEnemy;
    float _enemyMinDistance = 8f;
    public GameObject explosionPrefab;

    //Esfera y material apagado
    public Renderer sphereRenderer;
    public Material deathMaterial;
    public Material fresnelMaterial;

    [SerializeField] AudioClip _explosionClip;

    // Start is called before the first frame update
    void Awake()
    {
        _playerControls = GetComponent<PlayerControls>();
        _playerManager = GetComponent<PlayerManager>();
        _rb = GetComponentInChildren<Rigidbody>();
        _targetLockManager = GetComponentInChildren<TargetLock>();
        _turnSpeed = PlayerStats.turnSpeed;
        _sphereCollider = transform.GetComponent<SphereCollider>();
        _capsuleCollider = transform.GetComponent<CapsuleCollider>();

    }
    
    public void LaunchSphere(Vector3 previousMove) //Llamado cuando el motor explota desde PlayerManager
    {

        if (SceneManager.GetActiveScene().buildIndex == 2) //Si esta contra el boss final, se reinicia la partida
        {
            GameManager.Instance.FadeAndLoadScene(0);
            return;
        }

        if (sphereRenderer != null) sphereRenderer.material = fresnelMaterial;
        
        _rb.velocity = Vector3.zero;
        Vector3 launchForce = (Vector3.up + previousMove.normalized * 0.5f) * 9f;
        _rb.AddForce(launchForce, ForceMode.Impulse);
        actived = false;
        StartCoroutine(EnableMovementDelay(0.5f));
    }
    void Update()
    {
        _isGrounded = IsGrounded();
        if (actived) {

            if (!_isMorphing) {
                SetAllInputs();
                _lifeSecondTimer -= Time.deltaTime;
                GameUI.Instance.timerValue = _lifeSecondTimer; //Actualiza UI
                if (_lifeSecondTimer <= 0f) {
                    actived = false;
                    //_rb.freezeRotation = false; //La esfera se desactiva y puede rodar por el mapa
                    _playerManager.SetCompassSprite(false);

                    //Cambio de material (se apaga la esfera)
                    if (sphereRenderer != null && deathMaterial != null)
                        sphereRenderer.material = deathMaterial;

                    TrailRenderer trailRenderer = GetComponentInChildren<TrailRenderer>();
                    trailRenderer.enabled = false;

                    //Cae al suelo
                    _capsuleCollider.center = new Vector3(0f, 2.2f, 0f);

                    //IF FINAL BOSS = PERDER DEL TODO
                    //FALTA GAME OVER REAL (DESACTIVAR BRILLO DE ESFERA Y QUE CAIGA Y NO TE PUEDAS MOVER)
                    _lifeSecondTimer = 15f; //Reinicio del temporizador
                    GameManager.Instance.RestartPlayer();

                    

                }
                else
                {
                    _closestEnemy = _targetLockManager.GetClosestEnemy();
                    if (_closestEnemy != null && !_closestEnemy.CompareTag("FinalBoss") && !_closestEnemy.CompareTag("Drone"))
                    {
                        float distance = Vector3.Distance(_closestEnemy.transform.position, transform.position);
                        if(distance < _enemyMinDistance)
                        {
                            _playerControls.HandleButtonSprite(true);

                            if (_playerControls.isInteracting)
                            {
                                _isMorphing = true;
                                _playerControls.HandleButtonSprite(false);
                                PossessEnemy();
                                
                            }
                        }
                        else
                        {
                            _playerControls.HandleButtonSprite(false);
                        }


                       
                    }
                }
               

            }
        }   
    }

    private void FixedUpdate()
    {
        if (!actived) return;

        if (_isMorphing && _closestEnemy != null)
        {
            Vector3 currentPos = transform.position;
            Vector3 targetPos = _closestEnemy.transform.position + Vector3.up * 2f;

            float distanceToTarget = Vector3.Distance(currentPos, targetPos);

            if (distanceToTarget > 0.5f)
            {
                Vector3 toTarget = targetPos - currentPos;
                Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z);
                Vector3 directionXZ = toTargetXZ.normalized;

                float followSpeed = 12f;
                Vector3 horizontalVelocity = directionXZ * followSpeed;

                _rb.velocity = new Vector3(horizontalVelocity.x, _rb.velocity.y, horizontalVelocity.z);
            }
            else
            {
                _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);
                _isMorphing = false;
                Revive();
            }
        }

        else if(!_isMorphing)
        {
            if (_moveInput.magnitude > 0.01f)
            {
                Move();

            }

            else _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);

            AlignAndLook();

            if (_isGrounded)
            {
                if (_isJumping)
                {
                    Jump();
                }
                else
                {
                    _rb.velocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
                }
            }

            else
            {
                _turnSpeed = PlayerStats.turnSpeed * 0.4f;


            }
        }
    }

    void SetAllInputs()
    {
        _moveInput = new Vector3(_playerControls.moveVector.x, 0, _playerControls.moveVector.y);
        _isJumping = _playerControls.isJumping;
    }

    void Move()
    {
        //Movimiento en base a la cámara
        Vector3 moveDirection = GetDirectionRelativeToCamera(_moveInput);
        if (Vector3.Dot(moveDirection, previousMoveDirection) < 0.5f)
        {
            _currentSpeedMultiplier = 0.2f;
            _justFlipped = true;
        }

        _currentSpeedMultiplier = Mathf.MoveTowards(_currentSpeedMultiplier, 1f, _speedRecoveryRate * Time.deltaTime);
        Vector3 finalMove = moveDirection * _speed * _currentSpeedMultiplier;

        if (_isGrounded)
        {
            _rb.velocity = new Vector3(finalMove.x, _rb.velocity.y, finalMove.z);
        }
        else
        {
            // En el aire, un poco de inercia
            Vector3 airVelocity = new Vector3(
                finalMove.x / 1.25f,
                _rb.velocity.y,
                finalMove.z / 1.25f
            );
            _rb.velocity = Vector3.Lerp(_rb.velocity, airVelocity, 0.2f);
        }

        previousMoveDirection = moveDirection;
    }

    void AlignAndLook()
    {
        Vector3 moveDirection = GetDirectionRelativeToCamera(_moveInput);
        if (_justFlipped)
        {
            _justFlipped = false; // Reset after skipping one frame
            return;
        }

        Quaternion lookRotation;
        if (moveDirection.sqrMagnitude > 0.001f) lookRotation = Quaternion.LookRotation(moveDirection, transform.up); //look of the player
        else lookRotation = _rb.rotation; // no movement = no change

        //Align to the ground normal with the current rotation of rb
        Quaternion alignRotation = Quaternion.FromToRotation(transform.up, _groundNormal) * _rb.rotation;

        //Interpolate rotations
        Quaternion smoothedLook = Quaternion.RotateTowards(_rb.rotation, lookRotation, _turnSpeed * Time.fixedDeltaTime);
        Quaternion smoothedAlign = Quaternion.Slerp(_rb.rotation, alignRotation, 5f * Time.fixedDeltaTime);

        //Combine all into rigidbody rotation
        _rb.rotation = smoothedAlign * Quaternion.Inverse(_rb.rotation) * smoothedLook;
    }

    void Jump()
    {

        float _jumpSpeed = Mathf.Sqrt(-2 * PlayerStats.gravityScale * PlayerStats.jumpHeight);
        _rb.velocity = new Vector3(_rb.velocity.x, _jumpSpeed, _rb.velocity.z);

    }

    Vector3 GetDirectionRelativeToCamera(Vector3 inputDirection)
    {
        Camera cam = Camera.main;

        //Ignore y of isometric camera in order to obtain horizontal vectors (prevents player rotating to the ground)
        Vector3 camForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;

        return camForward * inputDirection.z + camRight * inputDirection.x;
    }

    Vector3 GetIsometricDirection(Vector3 direction)
    {
        Matrix4x4 matrix = Matrix4x4.Rotate(Quaternion.Euler(0, 45, 0));
        return matrix.MultiplyPoint3x4(direction);
    }

    bool IsGrounded()
    {
        RaycastHit hit;
        Vector3 raycastOrigin = new Vector3(transform.position.x, transform.position.y + 0.3f, transform.position.z);
        Ray ray = new Ray(raycastOrigin, -Vector3.up);

        Debug.DrawRay(raycastOrigin, -Vector3.up * 2f, Color.red); // Visualize the raycast in the scene view

        if (Physics.SphereCast(raycastOrigin, 0.2f, -Vector3.up, out hit, 0.6f, _groundMask)) // Adjust distance
        {

            if (Vector3.Angle(hit.normal, Vector3.up) > 1f) _groundNormal = hit.normal; //only for slopes
            else _groundNormal = Vector3.up; //only for flat ground

            _turnSpeed = PlayerStats.turnSpeed;
            return true;
        }


        _groundNormal = Vector3.up;
        return false;
    }

    public Vector3 GetLastMovementDirection()
    {
        return previousMoveDirection;
    }

    IEnumerator EnableMovementDelay(float time)
    {
        yield return new WaitForSeconds(time);
        actived = true;
    }

    void PossessEnemy()
    {
        CapsuleCollider cp = GetComponent<CapsuleCollider>();
        cp.enabled = false;
        _rb.velocity = Vector3.zero;
        float verticalForce = 12;
        _rb.AddForce(Vector3.up * verticalForce, ForceMode.Impulse); // Solo fuerza vertical
    } 

    void Revive()
    {
        //Animacion explotar y recuperar cuerpo
        PlayerStats.engineExploded = false;
        var explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        SoundFXManager.Instance.PlaySoundClip(_explosionClip, transform, 0.3f);
        Destroy(explosion, 3f);
        transform.position = _closestEnemy.transform.position;
        Destroy(_closestEnemy);
        CameraManager.Instance.ShakeCamera(0.25f, 15f);
        _playerManager.ResetPlayerModel();
        _playerManager.ResetPlayerHeat();

        //Stats del jugador y componentes reactivados
        PlayerStats.health = PlayerStats.maxHealth/2f;
        GameUI.Instance.ResetHealthUI();
        _lifeSecondTimer = 15f;
        _playerManager.ToggleMovement(true);
        _playerManager.ToggleCombat(true);
        _playerManager.ToggleEngine(true);
        _isMorphing = false;
        _playerManager.ToggleSphereController(false); //movimiento original del personaje

    }

}
