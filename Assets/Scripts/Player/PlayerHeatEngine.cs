using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class PlayerHeatEngine : MonoBehaviour
{
    PlayerManager _playerManager;
    PlayerControls _playerControls;

    //Engine Settings
    float _increaseRate = 4f;
    float _decreaseRate = 0.25f;
    float _increaseLerpSpeed = 5f;
    float[] _anchorPoints = new float[] { 0.5f, 1f, 1.5f };
    Color[] _engineColors = new Color[] { new Color(49, 178, 16), new Color(149, 116, 37), new Color(212, 7, 0) };
    Color[] _armorColors = new Color[] { new Color(83, 85, 159), new Color(161, 67, 63), new Color(185, 26, 20) };
    float _cooldownMaxDelay = 1.5f;
    float _cooldownAnchorDecrease = 5f;

    //Current engine
    float _currentHeat = 0f;
    float _targetHeat = 0f;
    int _anchorPointLevel = 1;
    bool _engineActived;
    float _cooldownTimer = 0f;
    float _anchorCooldownTimer = 0f;
    public bool engineExploded = false;

    //Stats
    float _speedMultiplier = 1.5f;
    float _damageMultiplier = 1.5f;
    float _explosionMultiplier = 1.5f;
    float _baseSpeedBonus = 1.0f;
    float _speedScaleFactor = 1.0f; // Valor incremental por unidad de calor
    float _baseDamageBonus = 1.0f;
    float _damageScaleFactor = 1.0f;
    float _baseAnimatorSpeed = 1.0f;
    float _animatorSpeedScaleFactor = 0.5f;

    //Climate (cuesta mas accionar el motor)
    public bool isNight;
    public bool raining;
    float _climateDecreaseRate = 1.5f;
    float _climateIncreaseRate = 0.5f;

    //Animator & materials
    Animator _playerAnimator;
    public Renderer coreRenderer;
    public GameObject explosionPrefab;
    public Transform swordPosition;
    public ParticleSystem fireSwordFX;
    public ParticleSystem[] slashAnimations;
    public Material originalSlashMat;
    public Material fireSlashMat;

    //Sound
    Coroutine _engineLoopCoroutine;
    [SerializeField] AudioSource _engineSource;
    [SerializeField] AudioClip _explosionClip;
    [SerializeField] AudioClip _activateEngineClip;
    [SerializeField] AudioClip _engineAmbientClip;


    private void Awake()
    {
        _playerAnimator = GetComponentInChildren<Animator>();
        _playerManager = GetComponent<PlayerManager>();
        _playerControls = GetComponent<PlayerControls>();
        _engineSource = GetComponent<AudioSource>();

    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.state == GameManager.GameState.Intro) return;
        Debug.Log("CALOR: " + _currentHeat);
        Debug.Log("Speed: " + PlayerStats.speed);

        if (_playerControls.isHeating) //Incremento
        {
            _engineActived = true;
            if (_anchorPointLevel == 3)
            {
                float explosionChance = Mathf.InverseLerp(_anchorPoints[2], 2.5f, _currentHeat);
                if (Random.value < explosionChance)
                {
                    ExplodeEngine();
                    return;
                }
            }

            float heatRate = _increaseRate;
            if (isNight || raining) heatRate *= _climateIncreaseRate;
            _targetHeat = _currentHeat + heatRate * Time.deltaTime;
            _cooldownTimer = _cooldownMaxDelay; //Segundos de delay para que empiece el decremento de potencia
            _anchorCooldownTimer = 0f; //Reinicia el tiempo sin motor

        }

        else //Decremento
        {
            if (_engineActived) _currentHeat = Mathf.Lerp(_currentHeat, _targetHeat, _increaseLerpSpeed * Time.deltaTime);
            if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
            else
            {
                _engineActived = false;
                float heatRate = _decreaseRate;
                if (isNight || raining) heatRate *= _climateDecreaseRate;
                _anchorPointLevel = GetAnchorPointLevel();
                int anchorLevel = Mathf.Clamp(_anchorPointLevel, 0, _anchorPoints.Length - 1);
                float anchorHeat = _anchorPoints[anchorLevel];
                _currentHeat = Mathf.Max(_currentHeat - heatRate * Time.deltaTime, 0f);
                if (_anchorPointLevel > 0 && _currentHeat <= _anchorPoints[_anchorPointLevel - 1])
                {
                    _currentHeat = _anchorPoints[_anchorPointLevel - 1];
                    
                    _anchorCooldownTimer += Time.deltaTime;
                    if (_anchorCooldownTimer >= _cooldownAnchorDecrease)
                    {
                        _anchorCooldownTimer = 0f;
                        if (_anchorPointLevel == 3) { //Para efecto de fuego
                            fireSwordFX.Stop();
                            for (int i = 0; i < slashAnimations.Length; i++)
                            {
                                ParticleSystemRenderer psr = slashAnimations[i].GetComponent<ParticleSystemRenderer>();
                                psr.material = originalSlashMat; //Vuelve al efecto original
                            }

                        }
                        else if (_anchorPointLevel == 1) _engineSource.Stop(); //Sin energia, para el sonido del motor
                        _anchorPointLevel--;
                        _currentHeat = (_anchorPointLevel > 0) ? _anchorPoints[_anchorPointLevel - 1] : 0f;

                    }
                }

            }



        }

        UpdateHeatStats();
        UpdateAnchorPoint();
        UpdateCoreColor();
        GameUI.Instance.UpdateHeatSlider(_currentHeat);
        HandleEngineSounds();


    }

    void UpdateAnchorPoint()
    {
        int previousLevel = _anchorPointLevel;
        if (_currentHeat >= _anchorPoints[2]) _anchorPointLevel = 3;
        else if (_currentHeat >= _anchorPoints[1]) _anchorPointLevel = 2;
        else if (_currentHeat >= _anchorPoints[0]) _anchorPointLevel = 1;
        else _anchorPointLevel = 0;

        if (previousLevel < 3 && _anchorPointLevel == 3)
        { //Espada en llamas y ataques
            fireSwordFX.Play();
            for (int i = 0; i < slashAnimations.Length; i++)
            {
                ParticleSystemRenderer psr = slashAnimations[i].GetComponent<ParticleSystemRenderer>();
                psr.material = fireSlashMat;
            }


        }
        
    }

    void UpdateHeatStats()
    {
        float speedBonus = _baseSpeedBonus + (_currentHeat * _speedScaleFactor);
        float damageBonus = _baseDamageBonus + (_currentHeat * _damageScaleFactor);
        float animSpeed = _baseAnimatorSpeed + (_currentHeat * _animatorSpeedScaleFactor);
        PlayerStats.animSpeed = animSpeed;
        PlayerStats.speed = PlayerStats.baseSpeed * speedBonus;
        PlayerStats.damage = PlayerStats.baseDamage * damageBonus;

        if (_playerAnimator != null)
            _playerAnimator.speed = animSpeed;
    }

    void UpdateCoreColor()
    {
        if (coreRenderer != null)
        {
            Color startColor = _engineColors[0];
            Color endColor = _engineColors[0];
            Color armorColor = _armorColors[0];
            Color targetArmorColor = _armorColors[0];
            float t = 0f;

            if (_currentHeat < _anchorPoints[0]) //Verde - Amarillo
            {
                startColor = _engineColors[0];
                endColor = _engineColors[1];
                armorColor = _armorColors[0];
                targetArmorColor = _armorColors[1];
                t = Mathf.InverseLerp(0f, _anchorPoints[0], _currentHeat);
            }
            else if (_currentHeat < _anchorPoints[1]) //Amarillo - Rojo
            {
                startColor = _engineColors[1];
                endColor = _engineColors[2];
                armorColor = _armorColors[1];
                targetArmorColor = _armorColors[2];
                t = Mathf.InverseLerp(_anchorPoints[0], _anchorPoints[1], _currentHeat);
            }

            else //Rojo
            {
                startColor = _engineColors[2];
                endColor = _engineColors[2];
                armorColor = _armorColors[2];
                targetArmorColor = _armorColors[2];
            }

            Color finalColor = Color.Lerp(startColor, endColor, t);
            coreRenderer.materials[3].SetColor("_EmissionColor", finalColor * 0.005f);

            Color finalArmorColor = Color.Lerp(armorColor, targetArmorColor, t);
            coreRenderer.materials[1].SetColor("_EmissionColor", finalArmorColor * 0.005f);

        }
    }

    public void ExplodeEngine()
    {
        Vector3 spawnPos = new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z);
        var explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        SoundFXManager.Instance.PlaySoundClip(_explosionClip, transform, 0.3f);
        _engineSource.Stop();
        Destroy(explosion, 3f);
        CameraManager.Instance.ShakeCamera(0.25f, 15f);
        //Llamar a cameraShake
        //Asignar nombre/tag player al nuevo
        GameObject playerModel = transform.GetChild(0).gameObject;
        playerModel.SetActive(false);
        GameObject sphereModel = transform.GetChild(1).gameObject;
        sphereModel.SetActive(true);
        PlayerStats.engineExploded = true;
        GameUI.Instance.ActivateMorphTimer(15f); //Temporizador post mortem del motor
        //_playerManager.ChangeCollider(true);
        _playerManager.ToggleMovement(false);
        _playerManager.ToggleCombat(false);
        _playerManager.ToggleSphereController(true); //nuevo movimiento
        _playerManager.ToggleEngine(false);


    }

    void HandleEngineSounds()
    {
        if (_engineActived && _playerControls.isHeating)
        {
            if (_engineLoopCoroutine != null) StopCoroutine(_engineLoopCoroutine);

            _engineSource.Stop(); //Para el sonido anterior
            StopCoroutine(PlayEngineLoopAfter(_activateEngineClip.length));
            _engineSource.clip = _activateEngineClip;
            _engineSource.loop = false;
            _engineSource.Play();


            if(_anchorPointLevel > 0) _engineLoopCoroutine = StartCoroutine(PlayEngineLoopAfter(_activateEngineClip.length)); //Loop del motor 
        }

        if (engineExploded && _engineSource.isPlaying)
        {
            _engineSource.Stop();
            if (_engineLoopCoroutine != null) StopCoroutine(_engineLoopCoroutine);
        }
    }

    private IEnumerator PlayEngineLoopAfter(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!_engineActived || engineExploded) yield break;

        _engineSource.clip = _engineAmbientClip;
        _engineSource.loop = true;
        _engineSource.Play();
    }

    int GetAnchorPointLevel()
    {
        for (int i = _anchorPoints.Length - 1; i >= 0; i--)
        {
            if (_currentHeat >= _anchorPoints[i])
                return i + 1;
        }
        return 0;
    }

    public void ResetEngineStats() //Se llama al morir
    {
        //Motor reiniciado
        _currentHeat = 0f;
        _targetHeat = 0f;
        _anchorPointLevel = 0;
        _cooldownTimer = 0f;
        _anchorCooldownTimer = 0f;
        _engineActived = false;
        engineExploded = false;

       
        if (_engineSource.isPlaying) _engineSource.Stop();
        if (_engineLoopCoroutine != null) StopCoroutine(_engineLoopCoroutine);

        //Apagar efectos visuales
        fireSwordFX.Stop();
        for (int i = 0; i < slashAnimations.Length; i++)
        {
            ParticleSystemRenderer psr = slashAnimations[i].GetComponent<ParticleSystemRenderer>();
            psr.material = originalSlashMat;
        }

        //Color original nucleo
        UpdateCoreColor();

        //Stats base
        PlayerStats.speed = PlayerStats.baseSpeed;
        PlayerStats.damage = PlayerStats.baseDamage;
        if (_playerAnimator != null)
            _playerAnimator.speed = _baseAnimatorSpeed;

        //Interfaz
        GameUI.Instance.UpdateHeatSlider(_currentHeat);

    }
}
