using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    PlayerControls _playerControls;
    public static CameraManager Instance;

    public Transform player;

    // Camera config
    public float distanceFromTarget = 5f;
    public float heightOffset = 2f;
    public Vector3 cameraOffset = Vector3.zero;
    public float followSpeed = 5f;

    // Rotation config
    private float targetAngle = 225f; // Smoothed Y rotation
    [SerializeField] private float rotationLerpSpeed = 2f; // Adjust for rotation smoothness
    private float currentAngle = 225f;
    public float mouseSensitivity = 2f;
    public float rotationSpeed = 2f;

    // Shake config
    private float _shakeDuration = 0f;
    private float _shakeMagnitude = 0.1f;
    private float _shakeFadeSpeed = 2f;
    private Vector3 _shakeOffset = Vector3.zero;

    private Vector3 smoothedPosition;

    // Lock-on
    private bool isLockedOn = false;
    private Transform lockedTarget;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else
        {
            Instance = this;
            //DontDestroyOnLoad(this);
        }

        _playerControls = FindFirstObjectByType<PlayerControls>();
    }

    void Update()
    {
        HandleInput();
        HandleShake();

        if (isLockedOn && lockedTarget != null)
        {
            //Locked mode (enemy target)
            Vector3 midpoint = (player.position + lockedTarget.position) / 2f;
            Vector3 directionToEnemy = (lockedTarget.position - midpoint).normalized;
            float desiredAngleY = Mathf.Atan2(directionToEnemy.x, directionToEnemy.z) * Mathf.Rad2Deg;

            targetAngle = Mathf.LerpAngle(targetAngle, desiredAngleY, rotationLerpSpeed * Time.deltaTime); //New angle
            Quaternion rotation = Quaternion.Euler(26.6f, targetAngle, 0f); //Siempre en x = 26.6
            Vector3 offset = rotation * new Vector3(0, 0, -distanceFromTarget);
            Vector3 desiredPosition = midpoint + offset + new Vector3(0, heightOffset, 0) + _shakeOffset;

            smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime); //Smooth
            transform.position = smoothedPosition;
            transform.rotation = rotation;
        }
        else
        {
            //Free mode
            currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
            CalculateFreeCameraPosition(currentAngle);
            transform.rotation = Quaternion.Euler(26.6f, currentAngle, 0f);
        }
    }

    void HandleInput()
    {
        if (isLockedOn) return;

        float lookInput = _playerControls.lookVector.x;
        float turnInput = _playerControls.turnValue;

        if (Mathf.Abs(lookInput) > 0.01f)
        {
            targetAngle += lookInput * mouseSensitivity;
        }
        else if (Mathf.Abs(turnInput) > 0.01f)
        {
            targetAngle += turnInput * mouseSensitivity;
        }
        else
        {
            targetAngle = Mathf.Round(targetAngle / 45f) * 45f;
        }

        targetAngle = (targetAngle + 360f) % 360f;
    }

    void HandleShake()
    {
        if (_shakeDuration > 0)
        {
            _shakeOffset = Random.insideUnitSphere * _shakeMagnitude;
            _shakeDuration -= Time.deltaTime * _shakeFadeSpeed;
        }
        else
        {
            _shakeOffset = Vector3.zero;
        }
    }

    void CalculateFreeCameraPosition(float angle)
    {
        Vector3 desiredPosition = player.position + new Vector3(
            Mathf.Sin(angle * Mathf.Deg2Rad) * distanceFromTarget,
            heightOffset,
            Mathf.Cos(angle * Mathf.Deg2Rad) * distanceFromTarget
        ) + cameraOffset + _shakeOffset;

        smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }

    public void ShakeCamera(float duration, float magnitude)
    {
        _shakeDuration = duration;
        _shakeMagnitude = magnitude;
    }

    public void SetLockOnTarget(Transform target) //TargetLock.cs
    {
        lockedTarget = target;
        isLockedOn = (target != null);
    }

    public void SearchPlayer()
    {
        player = GameObject.Find("Player").transform;
    }
}
