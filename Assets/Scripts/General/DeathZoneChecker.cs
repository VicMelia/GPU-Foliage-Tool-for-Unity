using UnityEngine;

public class DeathZoneChecker : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DeathZone"))
        {
            GameManager.Instance.RestartPlayer();
        }
    }

}