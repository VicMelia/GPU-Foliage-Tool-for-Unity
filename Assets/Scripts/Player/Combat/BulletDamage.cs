using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletDamage : MonoBehaviour
{
    float _damage = PlayerStats.bulletDamage;

    public GameObject bulletParticle;
    float _maxBulletTime = 3f;
    Vector3 enemyOrigin;

    private void Start()
    {
        Destroy(gameObject, _maxBulletTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("FinalBoss") || collision.gameObject.CompareTag("Drone"))
        {
            DamageEnemy(collision);
        }

        else if (collision.gameObject.CompareTag("Player"))
        {
            DamagePlayer(collision);
        }

        else Destroy(gameObject);


    }

    private void DamageEnemy(Collision collision)
    {
        Enemy enemy = collision.collider.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.GetDamage(_damage, false);
        }

        Transform player = GameObject.Find("Player").transform;
        Vector3 particleDirection = player.position - transform.position;
        var particle = Instantiate(bulletParticle, enemy.transform.position, Quaternion.LookRotation(particleDirection));
        ParticleSystem ps = particle.GetComponent<ParticleSystem>();
        ps.Play();
        var main = ps.main;
        float totalTime = main.duration + main.startLifetime.constantMax;
        Destroy(particle, totalTime);
        Destroy(gameObject);
    }

    private void DamagePlayer(Collision collision)
    {
        PlayerAttack pa = collision.collider.GetComponent<PlayerAttack>();
        if (pa != null)
        {
            _damage = 40f;
            pa.GetDamage(_damage, null);
        }

        Transform player = GameObject.Find("Player").transform;
        Vector3 particleDirection = player.position - transform.position;
        var particle = Instantiate(bulletParticle, enemyOrigin, Quaternion.LookRotation(particleDirection));
        ParticleSystem ps = particle.GetComponent<ParticleSystem>();
        ps.Play();
        var main = ps.main;
        float totalTime = main.duration + main.startLifetime.constantMax;
        Destroy(particle, totalTime);
        Destroy(gameObject);
    }

    public void SetEnemyOrigin(Vector3 origin)
    {
        enemyOrigin = origin;
    }

}
