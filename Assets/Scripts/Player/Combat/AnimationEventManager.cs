using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEventManager : MonoBehaviour
{

    public ParticleSystem[] slashAnimations;
    DamageDealer _damageDealer;
    PlayerAttack _playerAttack; //shooting
    [SerializeField] AudioClip _swordSlashClip;

    // Start is called before the first frame update
    void Start()
    {
        _damageDealer = transform.parent.GetComponentInChildren<DamageDealer>();
        _playerAttack = GetComponentInParent<PlayerAttack>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ActivateSlashVFX(int current)
    {
        slashAnimations[current].Play();
        SoundFXManager.Instance.PlaySoundClip(_swordSlashClip, transform, 0.05f);
    }

    public void DeactivateAllSlashes()
    {
        for(int i = 0; i <  slashAnimations.Length; i++)
        {
            slashAnimations[i].Stop();
        }
    }

    public void DamagePlayer()
    {
        _damageDealer.DoDamageToPlayer();
    }

    public void ActivateDamage(int hit)
    {
        bool inAir = GetComponent<Animator>().GetBool("Grounded");
        _damageDealer.DoDamage(hit, inAir);
    }

    public void GetDown()
    {
        _playerAttack.PullingDown();
    }

    
}
