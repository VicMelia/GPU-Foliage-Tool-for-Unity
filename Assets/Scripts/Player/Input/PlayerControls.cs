using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerControls : MonoBehaviour
{
    PlayerInput _playerInput;

    public Vector3 moveVector = Vector2.zero;
    public Vector2 lookVector = Vector2.zero;
    public float turnValue;

    public bool isJumping;
    public bool isDashing;

    public bool isAttacking;
    public bool isUpAttacking;
    public bool isShooting;
    public bool isHooking;
    public bool isLocking;
    public bool isParrying;
    public bool isHeating;

    public bool isInteracting;
    public bool isInMenu;
    public bool isAnyKey;


    //Gamepad/keyboard
    public enum ControlScheme
    {
        Keyboard,
        Xbox,
        PS4,
        Unknown
    }

    public ControlScheme CurrentControlScheme => DetectCurrentControlScheme();
    public Image inputButton;
    //Sprite buttons
    public Sprite keyboardSprite;
    public Sprite ps4Sprite;
    public Sprite xboxSprite;



    private void Awake()
    {
        _playerInput = new PlayerInput();
    }

    private void OnEnable()
    {
        _playerInput.Enable();

        // Movement
        _playerInput.Movement.Walk.performed += OnWalkPerformed;
        _playerInput.Movement.Walk.canceled += OnWalkCancelled;
        _playerInput.Movement.JumpDash.performed += OnJumpPerformed;
        _playerInput.Movement.JumpDash.canceled += OnJumpCancelled;
        _playerInput.Movement.Look.performed += OnLookPerformed;
        _playerInput.Movement.Look.canceled += OnLookCancelled;
        _playerInput.Movement.Turn.performed += OnTurnPerformed;
        _playerInput.Movement.Turn.canceled += OnTurnCancelled;

        // Combat
        _playerInput.Combat.Attack.performed += OnAttack;
        _playerInput.Combat.UpAttack.performed += OnUpAttack;
        _playerInput.Combat.Shoot.performed += OnShoot;
        _playerInput.Combat.Shoot.canceled += OnShootCancelled;
        _playerInput.Combat.Hook.performed += OnHook;
        _playerInput.Combat.Lock.performed += OnLock;
        _playerInput.Combat.Parry.performed += OnParry;
        //_playerInput.Combat.Parry.canceled += OnParryCancelled;

        // Overheat
        _playerInput.Overheat.Heat.performed += OnHeat;
        _playerInput.Overheat.Heat.canceled += OnHeatCancelled;

        // General
        _playerInput.General.Interact.performed += OnInteract;
        _playerInput.General.Interact.canceled += OnInteractCancelled;
        _playerInput.General.Menu.performed += OnMenu;
        _playerInput.General.Menu.canceled += OnMenuCancelled;
        // General
        _playerInput.General.AnyButton.performed += OnAnyKey;


    }

    private void OnDisable()
    {
        _playerInput.Disable();

        _playerInput.Movement.Walk.performed -= OnWalkPerformed;
        _playerInput.Movement.Walk.canceled -= OnWalkCancelled;
        _playerInput.Movement.JumpDash.performed -= OnJumpPerformed;
        _playerInput.Movement.JumpDash.canceled -= OnJumpCancelled;
        _playerInput.Movement.Look.performed -= OnLookPerformed;
        _playerInput.Movement.Look.canceled -= OnLookCancelled;
        _playerInput.Movement.Turn.performed -= OnTurnPerformed;
        _playerInput.Movement.Turn.canceled -= OnTurnCancelled;

        _playerInput.Combat.Attack.performed -= OnAttack;
        _playerInput.Combat.UpAttack.performed -= OnUpAttack;
        _playerInput.Combat.Shoot.performed -= OnShoot;
        _playerInput.Combat.Shoot.canceled -= OnShootCancelled;
        _playerInput.Combat.Hook.performed -= OnHook;
        _playerInput.Combat.Lock.performed -= OnLock;
        _playerInput.Combat.Parry.performed -= OnParry;
        //_playerInput.Combat.Parry.canceled -= OnParryCancelled;

        _playerInput.Overheat.Heat.performed -= OnHeat;
        _playerInput.Overheat.Heat.canceled -= OnHeatCancelled;

        _playerInput.General.Interact.performed -= OnInteract;
        _playerInput.General.Interact.canceled -= OnInteractCancelled;
        _playerInput.General.Menu.performed -= OnMenu;
        _playerInput.General.Menu.canceled -= OnMenuCancelled;
        _playerInput.General.AnyButton.performed -= OnAnyKey;

    }

    public void HandleButtonSprite(bool activate)
    {
        if (!activate)
        {
            inputButton.enabled = false;
            return;
        }

        Sprite sp;

        switch (CurrentControlScheme)
        { 
            case ControlScheme.Keyboard:
                sp = keyboardSprite;
                break;
            case ControlScheme.Xbox:
                sp = xboxSprite;
                break;
            case ControlScheme.PS4:
                sp = ps4Sprite;
                break;
            default:
                sp = keyboardSprite;
                break;
        }
        

        inputButton.enabled = true;
        inputButton.sprite = sp;
    }

    // Movement
    private void OnWalkPerformed(InputAction.CallbackContext value) => moveVector = (Vector3)value.ReadValue<Vector2>();
    private void OnWalkCancelled(InputAction.CallbackContext value) => moveVector = Vector3.zero;
    private void OnJumpPerformed(InputAction.CallbackContext value) => isJumping = value.performed;
    private void OnJumpCancelled(InputAction.CallbackContext value) => isJumping = false;
    private void OnLookPerformed(InputAction.CallbackContext value) => lookVector = value.ReadValue<Vector2>();
    private void OnLookCancelled(InputAction.CallbackContext value) => lookVector = Vector2.zero;
    private void OnTurnPerformed(InputAction.CallbackContext value) => turnValue = value.ReadValue<float>();
    private void OnTurnCancelled(InputAction.CallbackContext value) => turnValue = 0f;

    // Combat
    private void OnAttack(InputAction.CallbackContext value) => isAttacking = value.performed;
    private void OnUpAttack(InputAction.CallbackContext value) => isUpAttacking = value.performed;
    private void OnShoot(InputAction.CallbackContext value) => isShooting = value.performed;
    private void OnShootCancelled(InputAction.CallbackContext value) => isShooting = false;
    private void OnHook(InputAction.CallbackContext value) => isHooking = value.performed;
    private void OnLock(InputAction.CallbackContext value) => isLocking = value.performed;
    private void OnParry(InputAction.CallbackContext value) => isParrying = value.performed;
    //private void OnParryCancelled(InputAction.CallbackContext value) => isParrying = false;

    // Overheat
    private void OnHeat(InputAction.CallbackContext value) => isHeating = value.performed;
    private void OnHeatCancelled(InputAction.CallbackContext value) => isHeating = false;

    // General
    private void OnInteract(InputAction.CallbackContext value) => isInteracting = value.performed;
    private void OnInteractCancelled(InputAction.CallbackContext value) => isInteracting = false;
    private void OnMenu(InputAction.CallbackContext value) => isInMenu = value.performed;
    private void OnMenuCancelled(InputAction.CallbackContext value) => isInMenu = false;
    private void OnAnyKey(InputAction.CallbackContext value) => isAnyKey = value.performed;

    private ControlScheme DetectCurrentControlScheme()
    {
        string[] devices = Input.GetJoystickNames();

        if (devices.Length > 0 && !string.IsNullOrEmpty(devices[0]))
        {
            string name = devices[0].ToLower();

            if (name.Contains("xbox"))
                return ControlScheme.Xbox;
            if (name.Contains("dualshock") || name.Contains("wireless controller") || name.Contains("ps"))
                return ControlScheme.PS4;

            return ControlScheme.Unknown; //otro tipo de gamepad
        }

        return ControlScheme.Keyboard;
    }

    private void LateUpdate()
    {
        isAttacking = false;
        isUpAttacking = false;
        isLocking = false;
        isParrying = false;
        isHeating = false;
        isHooking = false;
        isAnyKey = false;

        if (inputButton != null && inputButton.enabled) inputButton.transform.forward = Camera.main.transform.forward;

    }



}
