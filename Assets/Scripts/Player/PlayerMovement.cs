using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    //Components
    PlayerControls _playerControls;
    PlayerManager _playerManager;
    Rigidbody _rb;
    TargetLock _targetLock;

    Vector3 _moveInput;
    //Stats
    float _speed;
    float _turnSpeed;

    //Movement speed
    float _currentSpeedMultiplier = 1f;
    float _speedRecoveryRate = 2f;
    public Vector3 previousMoveDirection = Vector3.zero;
    bool _justFlipped = false;

    //Dash
    float _dashSpeed;
    float _dashDuration;
    float _dashCD;
    float _dashTimer;
    public bool isDashing = false;

    //Jumping
    bool _isGrounded;
    public LayerMask _groundMask;
    Vector3 _groundNormal = Vector3.up;

    Animator _playerAnimator;
    int _hashInputX = Animator.StringToHash("InputX");
    int _hashInputY = Animator.StringToHash("InputY");

    GameObject _currentEnemy;

    //Sound
    [SerializeField] AudioClip _dashClip;

    // Start is called before the first frame update
    void Awake()
    {
        _playerControls = GetComponent<PlayerControls>();
        _playerManager = GetComponent<PlayerManager>();
        _rb = GetComponentInChildren<Rigidbody>();
        _speed = PlayerStats.speed;
        _turnSpeed = PlayerStats.turnSpeed;
        _dashSpeed = PlayerStats.dashSpeed;
        _dashDuration = PlayerStats.dashDuration;
        _dashCD = PlayerStats.dashCD;
        _dashTimer = _dashCD;
        _playerAnimator = GetComponentInChildren<Animator>();
        _targetLock = GetComponentInChildren<TargetLock>();
        //_groundMask = LayerMask.GetMask("Terrain");
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.state == GameManager.GameState.Intro) return;
        _currentEnemy = _targetLock.GetClosestEnemy();
        _speed = PlayerStats.speed;

        if (!isDashing && _playerControls.isJumping && _currentEnemy != null)
        {
            if (_dashTimer < _dashCD) return;
            StartCoroutine(Dash());
            return;
        }

        _dashTimer += Time.deltaTime;
        SetAllInputs();
        _isGrounded = IsGrounded();

       
        if (_playerControls.isJumping && _isGrounded && _currentEnemy == null)
        {
            Jump();
        }
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance.state == GameManager.GameState.Intro) return;
        if (isDashing) return;

        if (_moveInput.magnitude > 0.01f)
        {
            Move();

        }

        else _rb.velocity = new Vector3(0f, _rb.velocity.y, 0f);

        AlignAndLook();

        if (_isGrounded)
        {
            if (_playerControls.isJumping && !isDashing && _currentEnemy == null) //Sin enemigos se puede saltar, con enemigos se cambia por "Dash"
            {
                Jump();
            }
            else
            {
                _rb.velocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
            }
        }

        else {
            _turnSpeed = PlayerStats.turnSpeed * 0.4f;
        
        
        } 


    }

    void SetAllInputs()
    {
        _moveInput = new Vector3(_playerControls.moveVector.x, 0, _playerControls.moveVector.y);
        
    }

    void Move()
    {
        //Movimiento en base a la cámara
        Vector3 moveDirection = GetDirectionRelativeToCamera(_moveInput);
        _playerAnimator.SetFloat(_hashInputX, moveDirection.z);
        _playerAnimator.SetFloat(_hashInputY, moveDirection.x);
        //Si has dado la vuelta de golpe --> menos velocidad
        if (Vector3.Dot(moveDirection, previousMoveDirection) < 0.5f)
        {
            _currentSpeedMultiplier = 0.2f;
            _justFlipped = true;
        }

        //Ajuste de velocidad al disparar
        float targetMultiplier = _playerAnimator.GetBool("Shooting") ? 0.5f : 1f;
        _currentSpeedMultiplier = Mathf.MoveTowards(
            _currentSpeedMultiplier,
            targetMultiplier,
            _speedRecoveryRate * Time.fixedDeltaTime
        );

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

        if (_playerAnimator.GetBool("Shooting")) return;

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

    IEnumerator Dash() { 

        // Disable movement and combat
        isDashing = true;
        _playerManager.ToggleCombat(false);

        Vector3 dashDirection = previousMoveDirection;
        if (dashDirection == Vector3.zero)
            dashDirection = transform.forward; // default forward dash

        _rb.rotation = Quaternion.LookRotation(dashDirection.normalized, transform.up);

        float startTime = Time.time;
        _playerAnimator.Play("Dash");
        SoundFXManager.Instance.PlaySoundClip(_dashClip, transform, 0.4f);

        while (Time.time < startTime + _dashDuration)
        {
            _rb.velocity = dashDirection.normalized * _dashSpeed;
            yield return null;
        }

        _rb.velocity = Vector3.zero;

        // Re-enable movement and combat
        _playerManager.ToggleCombat(true);
        _dashTimer = 0f;
        isDashing = false;
        

    }

    void Jump()
    {

        float _jumpSpeed = Mathf.Sqrt(-2 * PlayerStats.gravityScale * PlayerStats.jumpHeight);
        _rb.velocity = new Vector3(_rb.velocity.x, _jumpSpeed, _rb.velocity.z);
        //_playerAnimator.SetTrigger("Jump");


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

    bool IsOnWall()
    {
        RaycastHit hit;
        Vector3 raycastOrigin = new Vector3(transform.position.x, transform.position.y + 0.3f, transform.position.z);
        if (Physics.SphereCast(raycastOrigin, 0.3f, Vector3.forward, out hit, 0.6f, _groundMask))
        {
            _groundNormal = hit.normal;
            return true;
        }

        return false;

    }

    bool IsGrounded()
    {
        RaycastHit hit;
        Vector3 raycastOrigin = new Vector3(transform.position.x, transform.position.y + 0.3f, transform.position.z);
        Ray ray = new Ray(raycastOrigin, -Vector3.up);

        Debug.DrawRay(raycastOrigin, -Vector3.up * 2f, Color.red); // Visualize the raycast in the scene view
        if (!_isGrounded && Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity, _groundMask)) // Máximo 5m para no lanzar infinito
        {
            if (hit.distance > 4f)
            {
                _playerAnimator.Play("Fall");
            }
        }

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

    private void LateUpdate()
    {
        _playerAnimator.SetFloat("Speed", _moveInput.magnitude);
        if(!isDashing && _currentEnemy == null) _playerAnimator.SetBool("Jumped", _playerControls.isJumping);
        _playerAnimator.SetBool("Grounded", _isGrounded);
    }

}