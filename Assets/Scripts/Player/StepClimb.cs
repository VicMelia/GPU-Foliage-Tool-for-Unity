using UnityEngine;

public class StepClimb : MonoBehaviour
{
    [SerializeField] float _stepHeight = 0.4f;           
    [SerializeField] float _stepSmooth = 0.1f;
    public LayerMask obstacleMask;
    public Transform rayOrigin; //Rayo desde el pivote (pies)
    Rigidbody _rb;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        ClimbStep(Vector3.forward);
        ClimbStep((Vector3.forward + Vector3.right).normalized);
        ClimbStep((Vector3.forward - Vector3.right).normalized);
    }

    void ClimbStep(Vector3 dir)
    {
        //Rayo suelo para detectar escalera
        if (Physics.Raycast(rayOrigin.position, transform.TransformDirection(dir), out RaycastHit hitLower, 0.5f, obstacleMask))
        {
            //Rayo techo (si no detecta nada sube la escalera)
            Vector3 upperRayOrigin = rayOrigin.position + Vector3.up * _stepHeight;
            if (!Physics.Raycast(upperRayOrigin, transform.TransformDirection(dir), 0.5f, obstacleMask))
            {
                _rb.position += new Vector3(0f, _stepSmooth, 0f);
            }
        }
    }
}