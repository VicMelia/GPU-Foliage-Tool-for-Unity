using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassLoader : MonoBehaviour
{
    public List<Terrain> allTerrains;
    public GameObject terrainParent;
    public List<GameObject> allStructures;
    public GameObject structureParent;
    Transform _player;
    public float loadDistance = 100f;
    public float structureLoadDistance = 100f;
    public float enemyLoadDistance = 80f;

    public float checkInterval = 0.5f;
    float _checkTimer = 0f;
    [SerializeField] float _objectsPerFrame = 20f;
    Coroutine _checkRoutine;

    // Start is called before the first frame update
    void Awake()
    {
        _player = transform.parent;
        if (terrainParent != null)
        {
            Terrain[] terrains = terrainParent.GetComponentsInChildren<Terrain>(true);
            allTerrains.AddRange(terrains);
        }

        if (structureParent != null)
        {
            Transform[] children = structureParent.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child != structureParent.transform)
                    allStructures.Add(child.gameObject);
            }
        }

        StartCoroutine(CheckTerrainsAndStructures());


    }

    // Update is called once per frame
    void Update()
    {

        _checkTimer += Time.deltaTime;
        if (_checkTimer >= checkInterval)
        {
            _checkTimer = 0f;

            if (_checkRoutine != null) StopCoroutine(_checkRoutine);
            _checkRoutine = StartCoroutine(CheckTerrainsAndStructures());
        }
    }

    IEnumerator CheckTerrainsAndStructures()
    {
        yield return new WaitForSeconds(0.2f);

        while (true)
        {
            int count = 0;

            //Terrains
            foreach (var t in allTerrains)
            {
                if (t == null) continue;

                Vector3 playerPosXZ = new Vector3(_player.position.x, 0f, _player.position.z);
                Vector3 targetPosXZ = new Vector3(t.transform.position.x, 0f, t.transform.position.z);
                float distSqr = (playerPosXZ - targetPosXZ).sqrMagnitude;
                bool shouldRender = distSqr < loadDistance * loadDistance;

                if (t.gameObject.activeSelf != shouldRender)
                    t.gameObject.SetActive(shouldRender);

                count++;
                if (count == _objectsPerFrame)
                    yield return null;
            }

            //Structures and enemies
            foreach (var obj in allStructures)
            {
                if (obj == null) continue;

                float distance = obj.CompareTag("Enemy") ? enemyLoadDistance : structureLoadDistance;
                Vector3 playerPosXZ = new Vector3(_player.position.x, 0f, _player.position.z);
                Vector3 targetPosXZ = new Vector3(obj.transform.position.x, 0f, obj.transform.position.z);
                float distSqr = (playerPosXZ - targetPosXZ).sqrMagnitude;
                bool shouldRender = distSqr < distance * distance;

                if (obj.activeSelf != shouldRender)
                    obj.SetActive(shouldRender);

                count++;
                if (count == _objectsPerFrame)
                    yield return null;
            }

            yield return new WaitForSeconds(0.2f);
        }
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Terrain"))
        {
            if (other.TryGetComponent<QMCTerrain>(out var instance))
            {
                instance.SetGrass();
            }
        }

        if (other.TryGetComponent<ComputeShaderTester>(out var i))
        {
            i.InstantiateSpheres();
        }

        if (other.gameObject.CompareTag("Checkpoint")) //Nuevo checkpoint
        {
            Debug.Log("Respawn");
            Debug.Log("OBJETO CHECK: " + other.gameObject);
            PlayerStats.lastCheckpoint = other.transform;
            Debug.Log("POSICION CHECK: " + PlayerStats.lastCheckpoint.position);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Terrain"))
        {
            if (other.TryGetComponent<QMCTerrain>(out var instance))
            {
                instance.ClearGrass();
            }
        }

        if (other.TryGetComponent<ComputeShaderTester>(out var i))
        {
            i.ClearGrass();
        }
    }
}
