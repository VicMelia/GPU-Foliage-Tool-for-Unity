using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerAttack : MonoBehaviour
{
    PlayerControls _playerControls;
    Animator _playerAnimator;
    bool _canAttack = true;
    bool _getHit;
    float _attackTime = 0.2f;
    float _recoverMovementCD = 0.6f;
    float _comboResetTime = 1f;

    //Input buffer
    float _inputBufferTime = 0.3f;
    private bool _bufferedAttack = false;
    private float _bufferTimer = 0f;

    bool _movingToEnemy = false;
    GameObject _closestEnemy;

    TargetLock _targetLockManager;
    PlayerManager _playerManager;
    Rigidbody _rb;

    //Hook
    int _hitCounter = 0;
    public Transform _hookSpawn;
    LineRenderer _lineRenderer;
    bool _isHooking;
    bool _pullingUp;
    bool _pullingDown;
    bool _forcePullDown;
    Vector3 _pullPosition;
    bool _hookHitted;
    float _maxHookDistance = 40f;
    float _hookCd = 3;
    float _hookCdTimer = 0;
    float _hookDelayTime = 0.3f;
    bool _isAnimatingHook = false;
    public LayerMask hookLayerMask;
    public LayerMask attackLayerMask;
    Vector3 _hookPoint;
    float _hookTimeout = 3f;

    //Parry-Block
    bool _blocking;
    bool _isParrying;
    float _parryTime = 0.3f;

    //Shoot
    bool _isShooting = false;
    public Transform bulletSpawn;
    public GameObject bulletPrefab;
    float _shootCooldown = 2f;
    float _shootCooldownTimer = 0f;
    //Shoot Blend Tree
    int _hashInputX = Animator.StringToHash("InputX");
    int _hashInputY = Animator.StringToHash("InputY");

    //Hit
    int _lastHitIndex = 0;
    bool _usedAirAttack = false;

    //Life regen
    float _regenDelay = 5f;
    float _regenTimer = 0f;

    private Coroutine _shootingRoutine;

    //Sound
    [SerializeField] AudioClip _parryClip;
    [SerializeField] AudioClip _bulletClip;
    [SerializeField] AudioClip _getHitClip;

    // Start is called before the first frame update
    void Start()
    {
        _playerControls = GetComponent<PlayerControls>();
        _playerAnimator = GetComponentInChildren<Animator>();
        _targetLockManager = GetComponentInChildren<TargetLock>();
        _playerManager = GetComponent<PlayerManager>();
        _rb = GetComponent<Rigidbody>();
        _lineRenderer = _hookSpawn.GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.state == GameManager.GameState.Intro) return;
        if (_getHit) return;

        if (_bufferedAttack)
        {
            _bufferTimer -= Time.deltaTime;
            if (_bufferTimer <= 0f)
                _bufferedAttack = false;
        }

        if (_playerAnimator.GetBool("Grounded") && _usedAirAttack)
        {
            _usedAirAttack = false;
        }

        if (!_blocking)
        {
            if (_playerControls.isParrying)
            {
                _closestEnemy = _targetLockManager.GetClosestEnemy();
                if (_closestEnemy != null)
                {
                    _playerManager.ToggleMovement(false);
                    _rb.velocity = Vector3.zero;
                    _blocking = true;
                    StartCoroutine(ParryAttack());
                    StartCoroutine(RecoverMovement(_recoverMovementCD * 2));
                }

            }

            else
            {
                if (_playerControls.isShooting && !_isShooting && _shootCooldownTimer <= 0f)
                {
                    _closestEnemy = _targetLockManager.GetClosestEnemy();
                    if (_closestEnemy != null) StartShooting();
                }
                if (_playerControls.isShooting && _isShooting)
                {
                    StopShooting();
                }

                if (_isShooting)
                {
                    UpdateShootRunBlendTree();
                    return;
                }



                if (!_isHooking)
                {
                    if (_playerControls.isHooking)
                    {
                        _closestEnemy = _targetLockManager.GetClosestEnemy();

                        if (_closestEnemy != null)
                        {

                            if (_closestEnemy.CompareTag("FinalBoss") || _closestEnemy.CompareTag("Drone")) return; //no puede enganchar al jefe final/dron
                            StartHook();
                        }
                    }
                    if (_hookCdTimer > 0) _hookCdTimer -= Time.deltaTime;

                    if (_playerControls.isAttacking)
                    {
                        if (_canAttack)
                        {
                            StartMeleeAttack();
                        }
                        else if (!_bufferedAttack)
                        {
                            if (!_playerAnimator.GetBool("Grounded") && (_hitCounter + 1) == 3)
                            {
                                _forcePullDown = true;
                            }

                            _bufferedAttack = true; //first hit of combo never buffers
                            _bufferTimer = _inputBufferTime;
                        }
                    }

                    if (_playerAnimator.GetBool("Grounded") && _playerControls.isUpAttacking && !_usedAirAttack && _canAttack)
                    {
                        _usedAirAttack = true;
                        _hitCounter = 0; //Resets hit combo
                        //StopCoroutine(WaitForNextAttack(_attackTime));
                        //StopCoroutine(RecoverMovement(_recoverMovementCD));
                        StopAllCoroutines(); //Negates movement recovery if still attacking
                        _closestEnemy = _targetLockManager.GetClosestEnemy();
                        if (_closestEnemy != null && Vector3.Distance(_closestEnemy.transform.position, transform.position) < 6f)
                        {
                            _playerManager.ToggleMovement(false);
                            _rb.velocity = Vector3.zero;
                            Vector3 lookDirection = _closestEnemy.transform.position - transform.position;
                            _rb.rotation = Quaternion.LookRotation(lookDirection.normalized, transform.up);
                            _playerAnimator.Play("UpAttack");
                            _canAttack = false;
                            if (!_closestEnemy.GetComponent<Enemy>()._attacking)
                            {

                                _playerAnimator.SetBool("Grounded", false);
                                StartCoroutine(PullingUpDelay(0.3f));
                            }

                            StartCoroutine(WaitForNextAttack(_attackTime));
                            StartCoroutine(RecoverMovement(_recoverMovementCD));
                        }


                    }

                }
            }
        }

        if (_shootCooldownTimer > 0f)
        {
            _shootCooldownTimer -= Time.deltaTime;
        }

        RegenerateHealth();




    }

    private void FixedUpdate()
    {
        if (GameManager.Instance.state == GameManager.GameState.Intro) return;
        if (_pullingUp || _pullingDown)
        {
            if (_closestEnemy == null) return;
            Vector3 finalPoint = _pullPosition;
            _closestEnemy.GetComponent<Enemy>().GetStunned();
            _rb.position = Vector3.Lerp(transform.position, finalPoint, 0.2f);
            Rigidbody enemyRb = _closestEnemy.GetComponent<Rigidbody>();
            Vector3 finalEnemyPosition = new Vector3(enemyRb.position.x, finalPoint.y, enemyRb.position.z);
            enemyRb.position = Vector3.Lerp(_closestEnemy.transform.position, finalEnemyPosition, 0.2f);

            if (Vector3.Distance(transform.position, finalPoint) < 1f)
            {
                _pullingUp = false;
                _pullPosition = Vector3.zero;
                _rb.position = transform.position;
                enemyRb.position = _closestEnemy.transform.position;
                if (_pullingDown)
                {
                    Vector3 launchDirection = _closestEnemy.transform.position - transform.position;
                    enemyRb.AddForce(launchDirection.normalized * 7f, ForceMode.Impulse);
                    _pullingDown = false;
                }
                _closestEnemy.GetComponent<Enemy>().GetStunned();


            }
        }



        else if (_movingToEnemy)
        {
            if (_closestEnemy == null) return;
            Vector3 lookDirection = _closestEnemy.transform.position - transform.position;
            Vector3 finalPoint = _closestEnemy.transform.position;
            if (_playerAnimator.GetBool("Grounded"))
            {
                lookDirection.y = 0f;
                finalPoint.y = transform.position.y;
            }

            _rb.rotation = Quaternion.LookRotation(lookDirection.normalized, transform.up);
            _rb.position = Vector3.Lerp(transform.position, finalPoint, 0.2f);


            //_rb.velocity = new Vector3(_rb.velocity.x, 0f, _rb.velocity.z);
            if (Vector3.Distance(transform.position, finalPoint) < 2.5f)
            {
                _movingToEnemy = false;
                _rb.position = transform.position;
                GameObject.Find("Slash7VFX").GetComponent<ParticleSystem>().Stop(); //Para el ultimo slash


            }

        }
    }

    private void LateUpdate()
    {
        if (_isHooking && !_hookHitted && !_isAnimatingHook)
        {
            _lineRenderer.SetPosition(0, _hookSpawn.position);
            _lineRenderer.SetPosition(1, _hookPoint);
        }

    }

    private void StartMeleeAttack()
    {
        _canAttack = false;
        _bufferedAttack = false;
        //StopCoroutine(WaitForNextAttack(_attackTime));
        //StopCoroutine(RecoverMovement(_recoverMovementCD));
        StopAllCoroutines();

        // Combo (1-2-3-1)
        _hitCounter++;
        if (_hitCounter >= 4) _hitCounter = 1;

        _playerManager.ToggleMovement(false);
        _closestEnemy = _targetLockManager.GetClosestEnemy();
        if (_closestEnemy != null)
        {
            Vector3 lookDir = _closestEnemy.transform.position - transform.position;
            lookDir.y = 0f;
            _rb.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);

            if (_playerAnimator.GetBool("Grounded"))
            {
                float dist = Vector3.Distance(transform.position, _closestEnemy.transform.position);

                if (dist > 6f && dist < 15f) //Se mueve automaticamente hacia el enemigo
                {
                    Vector3 directionToEnemy = (_closestEnemy.transform.position + Vector3.up) - (transform.position + Vector3.up); //up para subir los pivotes de los modelos
                    Ray ray = new Ray(transform.position + Vector3.up, directionToEnemy.normalized);
                    RaycastHit hit;

                    if (Physics.Raycast(ray, out hit, 15f, attackLayerMask)) //Evita que el jugador atraviese paredes para golpear al enemigo
                    {
                        Debug.Log("RAYCAST ATAQUE: " + hit.collider.gameObject);
                        if (hit.collider.gameObject == _closestEnemy)
                        {
                            _playerAnimator.Play("Run");
                            _playerAnimator.SetFloat("Speed", 0.1f);
                            _movingToEnemy = true;

                        }
                        else
                        {
                            _rb.AddForce(transform.forward * 5f, ForceMode.Impulse);
                        }

                    }
                }

                else if (dist > 3f) //Se mueve hacia donde esta mirando
                {
                    _rb.AddForce(transform.forward * 5f, ForceMode.Impulse);
                }
            }

            _rb.velocity = Vector3.zero;
        }
        else
        {
            if (PlayerStats.health < PlayerStats.maxHealth)
            {
                float regenRate = PlayerStats.lifeRegeneration * PlayerStats.healingMultiplier * Time.deltaTime;
                PlayerStats.health = Mathf.Min(PlayerStats.health + regenRate, PlayerStats.maxHealth);
                GameUI.Instance.UpdateHealthBar(PlayerStats.health);
            }

            _rb.velocity /= 2f;
            _rb.AddForce(transform.forward * 5f, ForceMode.Impulse);
        }

        if (PlayerStats.useBloodSeeker) PlayerStats.health -= 5f; //Espada Blood Seeker

        _playerAnimator.SetTrigger("Attack");
        StartCoroutine(WaitForNextAttack(_attackTime));
        StartCoroutine(RecoverMovement(_recoverMovementCD));
    }

    //Hook
    void StartHook()
    {
        if (_hookCdTimer > 0f) return;
        Debug.Log("GANCHO");
        _isHooking = true;
        _hitCounter = 0; //Resets combo
        RaycastHit hit;
        Vector3 direction = _closestEnemy.transform.position - transform.position;
        Debug.DrawRay(transform.position, direction, Color.red, 10f);
        Debug.Log("LayerMask int: " + hookLayerMask.value + " | Enemy Layer: " + LayerMask.NameToLayer("Outline"));
        if (Physics.Raycast(transform.position, direction, out hit, _maxHookDistance, hookLayerMask))
        {
            _playerManager.ToggleMovement(false); //Deactivates movement
            _rb.velocity = Vector3.zero;
            _playerAnimator.Play("ShootHook");//PlayAnimation1

            _rb.rotation = Quaternion.LookRotation(direction.normalized, transform.up);
            Debug.Log("GANCHO CONECTADO");
            _hookPoint = hit.point;
            _hookPoint = new Vector3(_hookPoint.x, _hookPoint.y + 1f, _hookPoint.z);
            _lineRenderer.enabled = true;
            _lineRenderer.positionCount = 2;
            StartCoroutine(AnimateHook(_hookSpawn.position, _hookPoint));

        }

        else StopHook();
    }

    void ExecuteHook()
    {
        _playerAnimator.SetBool("Hooked", true);  //PlayAnimation2: Retrieve animation
        Enemy enemy = _closestEnemy.GetComponent<Enemy>();
        enemy.GetDamage(0f, true);
        enemy.GetHooked();

        StartCoroutine(UpdateHookWhilePulling());
    }

    void StopHook()
    {
        _isHooking = false;
        _hookHitted = false;
        _isAnimatingHook = false;
        _pullingUp = false;
        _pullingDown = false;
        _forcePullDown = false;
        _hookCdTimer = _hookCd;
        _lineRenderer.enabled = false;
        _playerAnimator.SetBool("Hooked", false);
        _playerManager.ToggleMovement(true);
        _rb.velocity = Vector3.zero;
        _closestEnemy = null;
        _pullPosition = Vector3.zero;
        StartCoroutine(RecoverMovement(0.1f));

    }

    IEnumerator AnimateHook(Vector3 start, Vector3 end)
    {
        _isAnimatingHook = true;
        float duration = _hookDelayTime;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            Vector3 currentPos = Vector3.Lerp(start, end, t);
            _lineRenderer.SetPosition(0, _hookSpawn.position);
            _lineRenderer.SetPosition(1, currentPos);
            yield return null;
        }

        _lineRenderer.SetPosition(1, end);
        CameraManager.Instance.ShakeCamera(0.25f, 4f); //feedback
        _hookHitted = true;
        _isAnimatingHook = false;

        yield return null;

        ExecuteHook();
    }

    IEnumerator UpdateHookWhilePulling()
    {
        float elapsedTime = 0f;

        while (_closestEnemy != null && Vector3.Distance(_closestEnemy.transform.position, transform.position) > 2.5f)
        {
            _lineRenderer.SetPosition(0, _hookSpawn.position);
            _lineRenderer.SetPosition(1, _closestEnemy.transform.position);

            elapsedTime += Time.deltaTime;
            if (elapsedTime >= _hookTimeout)
            {
                Debug.LogWarning("Hook timeout. Forzando StopHook.");
                break;
            }

            yield return null;
        }

        if (!_playerAnimator.GetBool("Grounded"))
        {
            Rigidbody enemyRb = _closestEnemy.GetComponent<Rigidbody>();
            Vector3 playerVel = _rb.velocity;
            playerVel.y = 0f;
            _rb.velocity = playerVel;

            Vector3 enemyVel = enemyRb.velocity;
            enemyVel.y = 0f;
            enemyRb.velocity = enemyVel;
            _rb.AddForce(Vector3.up * 6f, ForceMode.Impulse);
            enemyRb.AddForce(Vector3.up * 6f, ForceMode.Impulse);
        }

        StopHook();
    }

    IEnumerator PullingUpDelay(float time)
    {
        yield return new WaitForSeconds(time);
        _pullPosition = new Vector3(transform.position.x, transform.position.y + 7f, transform.position.z);
        _pullingUp = true;

    }

    public void PullingDown()
    {
        RaycastHit playerHit;
        if (Physics.Raycast(transform.position, Vector3.down, out playerHit, Mathf.Infinity))
        {
            _pullPosition = playerHit.point;
            _pullingDown = true;

        }

    }

    public void GetDamage(float damage, Enemy currentEnemy)
    {
        if (_playerManager._playerMovement.isDashing) return;
        _playerManager.ToggleMovement(false);
        _rb.velocity = Vector3.zero;
        if (_isParrying && currentEnemy != null) //Se evalua si se hace parry
        {
            Debug.Log("LE HICE PARRY");
            CameraManager.Instance.ShakeCamera(0.25f, 10f);
            SoundFXManager.Instance.PlaySoundClip(_parryClip, transform, 0.4f);
            StartCoroutine(_playerManager._flashColor.Flash(0.2f, Color.white, Color.white)); //color feedback
            StartCoroutine(FreezeGame(0.2f, 0.7f)); //Ralentiza el tiempo al hacer parry
            if (currentEnemy.CompareTag("FinalBoss")) return;
            currentEnemy.DeactivateAnimationEffects();
            currentEnemy.GetStunned();
            currentEnemy.GetDamage(0f, true); //Solo para la animacion de recibir da�o
            return;
        }

        PlayerStats.health -= damage;
        SoundFXManager.Instance.PlaySoundClip(_getHitClip, transform, 0.4f);
        float h = PlayerStats.health;
        Debug.Log("VIDA ACTUAL: " + PlayerStats.health);
        _getHit = true;

        if (h <= 0f)
        {
            if (PlayerStats.engineExploded) //Si recibe da�o tras haber explotado, pierde
            {
                GameManager.Instance.RestartPlayer();
            }
            else
            {
                _playerManager._playerHeatEngine.ExplodeEngine();

            }

        }
        else
        {
            //Animacion de recibir golpe
            _playerAnimator.SetTrigger("GetHit");
            _playerAnimator.SetInteger("HitIndex", _lastHitIndex);
            if (_lastHitIndex == 0) _lastHitIndex = 1;
            else _lastHitIndex = 0;

            //Color rojo flash
            Color redColor = new Color(1f, 0.81f, 0.81f);
            redColor.a = 1f;
            Color emissionRedcolor = new Color32(207, 28, 39, 255);
            StartCoroutine(_playerManager._flashColor.Flash(0.3f, redColor, emissionRedcolor));

            CameraManager.Instance.ShakeCamera(0.25f, 4f); //damage feedback
            GameUI.Instance.UpdateHealthBar(PlayerStats.health);
            if (currentEnemy != null)
            {
                Vector3 direction = currentEnemy.transform.position - transform.position; //Mira hacia el enemigo
                direction.y = 0;
                Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
                _rb.rotation = rotation;
                _rb.AddForce(-direction.normalized * 2f, ForceMode.Impulse);
            }


            StartCoroutine(RecoverMovement(_recoverMovementCD + 0.1f));
            _getHit = false;


        }
    }

    private void RegenerateHealth()
    {
        if (_closestEnemy != null)
        {
            _regenTimer = 0f;
            return;
        }

        if (PlayerStats.health >= PlayerStats.maxHealth) return;

        _regenTimer += Time.deltaTime;
        if (_regenTimer >= _regenDelay)
        {
            Debug.Log("Me regenero");
            float regenRate = PlayerStats.lifeRegeneration * PlayerStats.healingMultiplier * Time.deltaTime;
            PlayerStats.health = Mathf.Min(PlayerStats.health + regenRate, PlayerStats.maxHealth);
            GameUI.Instance.UpdateHealthBar(PlayerStats.health);
        }
    }

    IEnumerator ParryAttack()
    {
        FaceEnemy();
        _playerAnimator.Play("Parry");
        _isParrying = true;
        float extraParryWindow = PlayerStats.extendParry ? 0.3f : 0f; //Se extiende el tiempo del parry con la espada "Blocker"
        yield return new WaitForSeconds(_parryTime + extraParryWindow);
        _isParrying = false;
    }

    IEnumerator FreezeGame(float delay, float time)
    {
        float originalFixedDelta = Time.fixedDeltaTime;

        Time.timeScale = delay; //Congela el tiempo
        Time.fixedDeltaTime = delay;
        yield return new WaitForSecondsRealtime(time);
        Time.timeScale = 1f; //Reanuda el tiempo
        Time.fixedDeltaTime = originalFixedDelta;
    }


    IEnumerator WaitForNextAttack(float time)
    {
        yield return new WaitForSeconds(time);
        if (_bufferedAttack)
        {
            //Segundo ataque guardado --> se ejecuta
            _bufferedAttack = false;
            StartMeleeAttack();
        }
        else
        {
            _canAttack = true;
        }
    }

    IEnumerator RecoverMovement(float time)
    {
        yield return new WaitForSeconds(time);
        _hitCounter = 0;
        _blocking = false;
        _playerManager.ToggleMovement(true);
    }

    void StartShooting()
    {
        if (_isShooting) return;

        _isShooting = true;
        _canAttack = false;
        _playerAnimator.SetTrigger("StartShoot");
        _playerAnimator.SetBool("Shooting", true); //for player movement script
        FaceEnemy();
        if (_shootingRoutine == null)
            _shootingRoutine = StartCoroutine(FireBullet());
    }

    void StopShooting()
    {
        if (!_isShooting) return;

        _isShooting = false;
        _canAttack = true;
        _playerAnimator.SetTrigger("StopShoot");
        _playerAnimator.SetBool("Shooting", false);
        StopCoroutine(FireBullet());

        _shootCooldownTimer = _shootCooldown; //para evitar 'spamear' el disparo
    }

    void UpdateShootRunBlendTree()
    {
        FaceEnemy();
    }
    //HELPER
    void FaceEnemy()
    {
        var enemy = _targetLockManager.GetClosestEnemy();
        if (enemy == null)
        {
            StopShooting();
            return;
        }
        Vector3 dir = enemy.transform.position - transform.position;
        dir.y = 0;
        _rb.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }

    /*
    public void FireBullet()
    {
        if (_closestEnemy == null)
            return;

        GameObject bullet = Instantiate(
            bulletPrefab,
            bulletSpawn.position,
            Quaternion.identity
        );
        CameraManager.Instance.ShakeCamera(0.25f, 4f); //feedback
        SoundFXManager.Instance.PlaySoundClip(_bulletClip, transform, 0.3f);
        Vector3 enemyPos = new Vector3(_closestEnemy.transform.position.x, _closestEnemy.transform.position.y + 1f, _closestEnemy.transform.position.z);
        Vector3 dir = (enemyPos - bulletSpawn.position).normalized;
        bullet.transform.forward = dir;
        if (bullet.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.velocity = dir * PlayerStats.bulletSpeed;
        }
    }
    */

    private IEnumerator FireBullet()
    {
        while (_playerControls.isShooting)
        {
            if (_closestEnemy == null)
            {
                _shootingRoutine = null;
                yield break;
            }

            GameObject bullet = Instantiate(
                bulletPrefab,
                bulletSpawn.position,
                Quaternion.identity
            );
            CameraManager.Instance.ShakeCamera(0.25f, 4f);
            SoundFXManager.Instance.PlaySoundClip(_bulletClip, transform, 0.3f);

            Vector3 enemyPos = _closestEnemy.transform.position + Vector3.up;
            Vector3 dir = (enemyPos - bulletSpawn.position).normalized;

            bullet.transform.forward = dir;
            if (bullet.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.velocity = dir * PlayerStats.bulletSpeed;
            }

            yield return new WaitForSeconds(0.75f);
        }
        _shootingRoutine = null;
    }



}