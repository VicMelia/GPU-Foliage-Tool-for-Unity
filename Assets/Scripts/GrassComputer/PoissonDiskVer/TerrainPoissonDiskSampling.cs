using System.Collections.Generic;
using UnityEngine;

public class TerrainPoissonDiskSampling : MonoBehaviour
{
    public ComputeShader poissonDiskComputeShader;
    public GameObject grassPrefab;
    public Terrain terrain;
    public int maxPoints = 1000;

    private ComputeBuffer pointsBuffer;
    private ComputeBuffer normalsBuffer;
    private ComputeBuffer counterBuffer;
    private List<Vector3> generatedPoints = new List<Vector3>();
    private List<Vector3> normalPoints = new List<Vector3>();
    private Material instanceMaterial;
    private List<Matrix4x4> instanceTransforms = new List<Matrix4x4>();
    private RenderTexture heightmapTexture;
    public float maxAngle = 90f;

    //Splatmap layers
    float[,,] splatmaps;
    private int grassIndexLayer = 0;

    public float targetRadius;
    private Vector3 terrainSize;
    [SerializeField] float scaleX = 1f;
    [SerializeField] float scaleY = 1f;
    [SerializeField] float scaleZ = 1f;

    int contador = 0;
    private MaterialPropertyBlock propBlock;
    Transform playerTransform;

    void Start()
    {
        /*
        terrain = GetComponent<Terrain>();
        instanceMaterial = grassPrefab.GetComponent<MeshRenderer>().sharedMaterial;
        propBlock = new MaterialPropertyBlock();
        terrain = GetComponent<Terrain>();
        playerTransform = FindFirstObjectByType<PlayerMovement>().transform;

        TerrainData terrainData = terrain.terrainData;
        terrainSize = terrainData.size;

        Vector3 terrainPosition = terrain.transform.position;


        //Heightmap for the compute buffer
        heightmapTexture = new RenderTexture(
            terrainData.heightmapResolution,
            terrainData.heightmapResolution,
            0,
            RenderTextureFormat.RFloat
        );
        heightmapTexture.enableRandomWrite = true;
        heightmapTexture.Create();

        //Terrain heightmap texture to the recently render texture
        Graphics.Blit(terrainData.heightmapTexture, heightmapTexture);

        splatmaps = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);

        // Initialize compute buffers and parameters
        pointsBuffer = new ComputeBuffer(maxPoints, sizeof(float) * 3, ComputeBufferType.Append);
        normalsBuffer = new ComputeBuffer(maxPoints, sizeof(float) * 3, ComputeBufferType.Append);
        counterBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Raw);

        pointsBuffer.SetCounterValue(0);
        normalsBuffer.SetCounterValue(0);
        counterBuffer.SetData(new uint[] { 0 });

        
        int kernelIndex = poissonDiskComputeShader.FindKernel("CSMain");
        poissonDiskComputeShader.SetTexture(kernelIndex, "heightmap", heightmapTexture);
        poissonDiskComputeShader.SetBuffer(kernelIndex, "points", pointsBuffer);
        poissonDiskComputeShader.SetBuffer(kernelIndex, "normals", normalsBuffer);
        poissonDiskComputeShader.SetBuffer(kernelIndex, "counterBuffer", counterBuffer);
        poissonDiskComputeShader.SetInt("terrainGridSizeX", terrainData.heightmapResolution);
        poissonDiskComputeShader.SetInt("terrainGridSizeZ", terrainData.heightmapResolution);
        poissonDiskComputeShader.SetVector("terrainSize", terrainSize);
        poissonDiskComputeShader.SetFloat("targetRadius", targetRadius);
        poissonDiskComputeShader.SetVector("terrainPosition", terrainPosition);
        poissonDiskComputeShader.SetInt("pointBufferLength", maxPoints);

        //Dispatch
        int threadGroups = Mathf.CeilToInt((float)maxPoints / 256);
        poissonDiskComputeShader.Dispatch(kernelIndex, threadGroups, 1, 1);

        //Retrieve points
        RetrieveGeneratedPoints();

        //Instantiate grass
        //InstantiateGrass();
        */
    }

    public void SetGrass()
    {
        InstantiateGrass();
    }

    public void ClearGrass()
    {
        instanceTransforms.Clear();
    }

    

    private void RetrieveGeneratedPoints()
    {
        //Count of valid points
        uint[] counterValue = new uint[1];
        counterBuffer.GetData(counterValue);
        int validPointCount = (int)counterValue[0];

        //Retrieve points from buffer
        Vector3[] points = new Vector3[validPointCount];
        pointsBuffer.GetData(points);

        generatedPoints.AddRange(points);

        Vector3[] normals = new Vector3[validPointCount];
        normalsBuffer.GetData(normals);
        normalPoints.AddRange(normals);
    }

    private void InstantiateGrass()
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPosition = terrain.transform.position;  

        for(int i = 0; i < generatedPoints.Count; i++)
        {
    
            float normalizedX = (generatedPoints[i].x - terrainPosition.x) / terrainData.size.x;
            float normalizedZ = (generatedPoints[i].z - terrainPosition.z) / terrainData.size.z;

            //Clamp within heightmap
            normalizedX = Mathf.Clamp01(normalizedX);
            normalizedZ = Mathf.Clamp01(normalizedZ);

            //Get Y from X and Z values
            float y = terrainData.GetHeight((int)(normalizedX * terrainData.heightmapResolution), 
                                            (int)(normalizedZ * terrainData.heightmapResolution));
            //New point with new X,Y,Z
            Vector3 worldPoint = new Vector3(generatedPoints[i].x, y, generatedPoints[i].z);
            Vector3 normalTerrain = terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);
            float slopeAngle = Vector3.Angle(normalTerrain, Vector3.up);
            if(slopeAngle > maxAngle) continue;
            if(IsOnGrassLayer(worldPoint)){
                
                Vector3 worldNormal = transform.TransformDirection(normalPoints[i]);
                //Rotation fix for the normals
                Quaternion rotation = Quaternion.LookRotation(Vector3.Cross(worldNormal, Vector3.right), worldNormal);
                float randomScaleY = Random.Range(scaleY - 0.2f, scaleY + 0.4f);
                Matrix4x4 transformMatrix = Matrix4x4.TRS(
                    worldPoint,
                    rotation, 
                    new Vector3(scaleX, randomScaleY, scaleZ)
                );

                contador++;
                instanceTransforms.Add(transformMatrix);

            }
           
        }
        Debug.Log("PUNTOS SHADER: " + contador);
    }

    bool IsOnGrassLayer(Vector3 worldPoint)
    {
        Terrain terrain = Terrain.activeTerrain; // Ensure you reference your terrain
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainLocalPos = worldPoint - terrain.transform.position;

        // Normalize terrain coordinates
        float xNormalized = terrainLocalPos.x / terrainData.size.x;
        float zNormalized = terrainLocalPos.z / terrainData.size.z;

        // Get splatmap coordinates
        int mapX = Mathf.FloorToInt(xNormalized * terrainData.alphamapWidth);
        int mapZ = Mathf.FloorToInt(zNormalized * terrainData.alphamapHeight);

        // Ensure coordinates are within bounds
        mapX = Mathf.Clamp(mapX, 0, terrainData.alphamapWidth - 1);
        mapZ = Mathf.Clamp(mapZ, 0, terrainData.alphamapHeight - 1);

        // Check if GrassLayer is dominant
        return splatmaps[mapZ, mapX, grassIndexLayer] > 0.5f;
    }

    private void Update()
    {

        propBlock.SetFloat("_PlayerY", playerTransform.position.y);

        if (instanceTransforms.Count > 0)
        {
            int batchSize = 1023; //Max batches
            for (int i = 0; i < instanceTransforms.Count; i += batchSize)
            {
                int count = Mathf.Min(batchSize, instanceTransforms.Count - i);

                Graphics.DrawMeshInstanced(
                    grassPrefab.GetComponent<MeshFilter>().sharedMesh,
                    0,
                    instanceMaterial,
                    instanceTransforms.GetRange(i, count),
                    propBlock
                );
            }
        }
    }

    private void OnDestroy()
    {
        // Release compute buffers
        /*
        pointsBuffer.Release();
        counterBuffer.Release();
        normalsBuffer.Release();

        if (heightmapTexture != null)
        {
            heightmapTexture.Release();
        }
        */
    }
}
