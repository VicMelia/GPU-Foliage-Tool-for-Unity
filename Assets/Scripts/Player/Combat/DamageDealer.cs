using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageDealer : MonoBehaviour
{

    List<GameObject> enemiesInRange = new List<GameObject>();
    bool _playerDetected;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DoDamage(int hit, bool inAir)
    {
       if (enemiesInRange.Count == 0) return;
       for(int i = 0; i < enemiesInRange.Count; i++)
       {
            if (enemiesInRange[i] == null) {
                enemiesInRange.RemoveAt(i); //if it's dead
                return;
            } 

            float damage = PlayerStats.damage;
            if (hit == 5 || hit == 6)
            {
                Rigidbody enemyRb = enemiesInRange[i].GetComponent<Rigidbody>();
                Rigidbody playerRb = transform.parent.transform.parent.GetComponent<Rigidbody>();

                // Reset vertical velocity for consistency
                Vector3 playerVel = playerRb.velocity;
                playerVel.y = 0f;
                playerRb.velocity = playerVel;

                Vector3 enemyVel = enemyRb.velocity;
                enemyVel.y = 0f;
                enemyRb.velocity = enemyVel;

                // Apply upward "momentum" force
                playerRb.AddForce(Vector3.up * 6f, ForceMode.Impulse);
                enemyRb.AddForce(Vector3.up * 6f, ForceMode.Impulse);

                CameraManager.Instance.ShakeCamera(0.25f, 10f);
            }

            else if (hit == 4 || hit == 7) //Up/down attack
            {
                damage *= 1.25f;
                damage += PlayerStats.gladiatorBonus;
                //Rigidbody playerRb = transform.parent.GetComponent<Rigidbody>();
                //Rigidbody enemyRb = enemiesInRange[i].GetComponent<Rigidbody>();
                //playerRb.AddForce(playerRb.transform.up * 10f, ForceMode.Impulse);
                //enemyRb.AddForce(enemyRb.transform.up * 10f, ForceMode.Impulse);
                CameraManager.Instance.ShakeCamera(0.25f, 12f);

            }

            else if (hit == 3) //Last hit combo does x1.5 damage and launches enemies away
            {
                damage *= 1.5f;
                damage += PlayerStats.gladiatorBonus;
                Rigidbody enemyRb = enemiesInRange[i].GetComponent<Rigidbody>();
                //Debug.Log("RIGIDBODY: " + enemyRb);
                Vector3 playerPosition = transform.parent.position;
                Vector3 launchDirection = enemiesInRange[i].transform.position - playerPosition;
                enemyRb.AddForce(launchDirection.normalized * 5f, ForceMode.Impulse);
                CameraManager.Instance.ShakeCamera(0.25f, 12f);

            }

            else CameraManager.Instance.ShakeCamera(0.25f, 4f);

            //Efectos espada
            Enemy enemy = enemiesInRange[i].GetComponent<Enemy>();
            //if (PlayerStats.markEnemy && enemy.IsMarked) damage *= 1.5f;
            if (PlayerStats.freezeOnHit) enemy.Freeze(2f); 
            if (PlayerStats.burnOnHit) enemy.Burn(5f, 3f);
            enemy.GetDamage(damage, true);


       }
    }

    public void DoDamageToPlayer()
    {
        if(!_playerDetected) return;
        Enemy e = transform.parent.GetComponent<Enemy>();
        PlayerAttack pa = GameObject.Find("Player").GetComponent<PlayerAttack>();
        pa.GetDamage(e.damage, e);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy") || other.gameObject.CompareTag("FinalBoss"))
        {
            enemiesInRange.Add(other.gameObject);
           
        }

        if (other.gameObject.CompareTag("Player"))
        {
            _playerDetected = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy") || other.gameObject.CompareTag("FinalBoss"))
        {
            enemiesInRange.Remove(other.gameObject);

        }

        if (other.gameObject.CompareTag("Player"))
        {
            _playerDetected = false;
        }
    }
}
