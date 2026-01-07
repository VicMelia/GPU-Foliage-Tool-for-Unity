using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TargetLock : MonoBehaviour
{
    PlayerControls _playerControls;
    PlayerAttack _playerAttack;
    private List<GameObject> enemiesInRange = new List<GameObject>();
    private GameObject lastClosestEnemy;
    bool _locked = false;
    bool _wasInCombat;

    private void Awake()
    {
        _playerControls = GetComponentInParent<PlayerControls>();
    }

    private void Update()
    {
        UpdateClosestEnemy();
       
        if (_playerControls.isLocking)
        {
            if (!_locked)
            {
                if (lastClosestEnemy != null)
                {
                    _locked = true;
                    CameraManager.Instance.SetLockOnTarget(lastClosestEnemy.transform);
                    ToggleIndicator(lastClosestEnemy, true);



                }
            }

            else
            {
                
                ToggleIndicator(lastClosestEnemy, false);
                int currentIndex = enemiesInRange.IndexOf(lastClosestEnemy);
                currentIndex++;
                if (currentIndex == enemiesInRange.Count) currentIndex = 0;
                lastClosestEnemy = enemiesInRange[currentIndex];
                ToggleIndicator(lastClosestEnemy, true);
                CameraManager.Instance.SetLockOnTarget(lastClosestEnemy.transform);


            }
            
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy") || other.gameObject.CompareTag("FinalBoss") || other.gameObject.CompareTag("Drone"))
        {
            if (other.GetComponent<Enemy>().IsDead()) return;
            if (enemiesInRange.Count == 0 && !other.gameObject.CompareTag("FinalBoss")) MusicManager.Instance.EnterCombatMusic();
            enemiesInRange.Add(other.gameObject);
            
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy") || other.gameObject.CompareTag("FinalBoss") || other.gameObject.CompareTag("Drone"))
        {
            if (_locked && lastClosestEnemy == other.gameObject) {


                _locked = false;
                CameraManager.Instance.SetLockOnTarget(null);
            }
           
            enemiesInRange.Remove(other.gameObject);
            if (enemiesInRange.Count == 0) lastClosestEnemy = null;
        }
    }

    private void UpdateClosestEnemy()
    {
        // Clean up dead enemies
        if (lastClosestEnemy == null) {

            _locked = false;
            CameraManager.Instance.SetLockOnTarget(null);
        } 
        enemiesInRange.RemoveAll(enemy => enemy == null || enemy.GetComponent<Enemy>().IsDead());
        if (enemiesInRange.Count == 0)
        {
            if (_wasInCombat) //Solo si antes había enemigos
            {
                _wasInCombat = false;
                Debug.Log("Saliste del combate");
                MusicManager.Instance.ExitCombatMusic();
            }

            if (_locked)
            {
                _locked = false;
                CameraManager.Instance.SetLockOnTarget(null);
            }

            return;
        }
        else
        {
            _wasInCombat = true;
        }

        if (_locked) return; // If locked, don't update the target


        float closestDistance = Mathf.Infinity;
        Vector3 currentPosition = transform.position;
        GameObject newClosestEnemy = null;

        for (int i = 0;  i < enemiesInRange.Count; i++)
        {
            
            Vector3 directionToTarget = enemiesInRange[i].transform.position - currentPosition;
            float distance = directionToTarget.sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                newClosestEnemy = enemiesInRange[i];
            }
        }

        //New indicator
        if (lastClosestEnemy != null) ToggleIndicator(lastClosestEnemy, false);
        if (newClosestEnemy != null) ToggleIndicator(newClosestEnemy, true);
        lastClosestEnemy = newClosestEnemy;
    }

    public GameObject GetClosestEnemy()
    {
        return lastClosestEnemy;
    }

    private void ToggleIndicator(GameObject enemy, bool isActive)
    {
        Transform indicator = enemy.transform.Find("WorldCanvas/LockImage");
        //indicator.transform.LookAt(Camera.main.transform);
        if (indicator != null) {


            Image uiImage = indicator.GetComponent<Image>();
            indicator.gameObject.SetActive(isActive);
            if (_locked)
            {
                uiImage.color = new Color(1f, 1f, 1f, 0.8f);
                indicator.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            }
            else
            {
                uiImage.color = new Color(1f, 1f, 1f, 0.3f);
                indicator.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            }


        } 
    }

    void LateUpdate()
    {
        if (lastClosestEnemy == null) return;
        Transform indicator = lastClosestEnemy.transform.Find("WorldCanvas/LockImage");
        Debug.Log(indicator);
        if (indicator != null && Camera.main != null)
        {
            indicator.transform.forward = Camera.main.transform.forward;

        }

    }


}
