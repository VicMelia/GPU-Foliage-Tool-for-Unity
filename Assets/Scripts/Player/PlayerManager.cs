using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{

    public PlayerMovement _playerMovement;
    public PlayerAttack _playerAttack;
    public PlayerHeatEngine _playerHeatEngine;
    public PlayerSphereController _playerSphereController;
    public FlashColor _flashColor;

    CapsuleCollider _capsuleCollider;
    SphereCollider _sphereCollider;
    public SpriteRenderer spriteCompass;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        _playerAttack = GetComponent<PlayerAttack>();
        _playerHeatEngine = GetComponent<PlayerHeatEngine>();
        _playerSphereController = GetComponent<PlayerSphereController>();
        _flashColor = GetComponent<FlashColor>();
        _capsuleCollider = GetComponent<CapsuleCollider>();
        _sphereCollider = GetComponent<SphereCollider>();
    }

    private void OnDisable()
    {
        _playerMovement = null;
        _playerAttack = null;
        _playerHeatEngine = null;
        _playerSphereController = null;
        _flashColor = null;
        _capsuleCollider = null;
        _sphereCollider = null;


    }

    public void ToggleMovement(bool isActive)
    {
        _playerMovement.enabled = isActive;
    }

    public void ToggleCombat(bool isActive)
    {
        _playerAttack.enabled = isActive;
    }

    public void ToggleEngine(bool isActive)
    {
        _playerHeatEngine.enabled = isActive;
    }

    public void ToggleSphereController(bool isActive)
    {
        _playerSphereController.enabled = isActive;
        _playerSphereController.LaunchSphere(_playerMovement.GetLastMovementDirection());
    }

    public void ChangeCollider(bool normal) //Llamado al revivir y al morir
    {
        if (normal)
        {
            _capsuleCollider.enabled = false;
            _sphereCollider.enabled = true;
        }
        else
        {
            _capsuleCollider.enabled = true;
            _sphereCollider.enabled = false;
        }
    }

    public void ResetCollider() //Llamado desde GameManager al morir
    {
        ChangeCollider(false);
        _capsuleCollider.center = new Vector3(0f, 1.42f, 0f);
    }

    public void ResetPlayerModel()
    {
        GameObject sphereModel = transform.GetChild(1).gameObject;
        sphereModel.SetActive(false);
        TrailRenderer trailRenderer = sphereModel.GetComponent<TrailRenderer>();
        trailRenderer.enabled = true;
        CapsuleCollider cp = GetComponent<CapsuleCollider>();
        cp.enabled = true;
        GameObject playerModel = transform.GetChild(0).gameObject;
        playerModel.SetActive(true);
    }

    public void ResetPlayerHeat()
    {
        _playerHeatEngine.ResetEngineStats();
    }

    public void SetCompassSprite(bool active)
    {
        spriteCompass.enabled = active;
    }

}
