using UnityEngine;

public class WorldCompass : MonoBehaviour
{
    [SerializeField] Transform[] _checkpoints;
    public Transform target;
    public Transform player; 
    public Transform arrow;

    private void Update()
    {
        target = GetNearestTarget();
        
    }

    void LateUpdate()
    {
        transform.position = new Vector3(player.position.x, player.position.y + 1f, player.position.z);

        if (target == null) return;

        Vector3 dir = target.position - player.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion rot = Quaternion.LookRotation(dir);
            arrow.localRotation = Quaternion.Euler(90f, rot.eulerAngles.y, 0f);
        }
    }

    Transform GetNearestTarget()
    {
        float distance = Mathf.Infinity;
        int count = 0;

        for(int i = 0; i < _checkpoints.Length; i++)
        {
            float newDistance = Vector3.Distance(player.position, _checkpoints[i].position);
            if(newDistance < distance)
            {
                distance = newDistance;
                count = i;
            }
        }

        return _checkpoints[count];
    }
}
