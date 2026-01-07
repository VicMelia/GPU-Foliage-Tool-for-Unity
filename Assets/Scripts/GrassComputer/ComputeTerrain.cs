using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeTerrain : MonoBehaviour
{
    public ComputeShader poissonDiskComputeShader; // Assign your compute shader

    public Terrain terrain; // Reference to the terrain
    public GameObject grassPrefab;
    [SerializeField] float scaleX = 1f;
    [SerializeField] float scaleY = 1f;
    [SerializeField] float scaleZ = 1f;

    public int maxPoints = 1000;
    //public float targetRadius = 1.0f; // Radius for sampling
    public int grassLayerIndex = 0; // Index of the grass layer in the splatmap

    private ComputeBuffer pointsBuffer;
    private ComputeBuffer counterBuffer;

    private List<Vector3> generatedPoints = new List<Vector3>();
    private Material instanceMaterial;
    private List<Matrix4x4> instanceTransforms = new List<Matrix4x4>();

    void Start()
    {
        // Get the material of the grass prefab
        instanceMaterial = grassPrefab.GetComponent<MeshRenderer>().sharedMaterial;

        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainSize = terrainData.size;

        // Compute shader buffers
        pointsBuffer = new ComputeBuffer(maxPoints, sizeof(float) * 3, ComputeBufferType.Append);
        counterBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Raw);

        // Initialize buffers
        pointsBuffer.SetCounterValue(0);
        counterBuffer.SetData(new uint[] { 0 });

        // Set compute shader parameters
        poissonDiskComputeShader.SetBuffer(0, "outputPoints", pointsBuffer);
        poissonDiskComputeShader.SetBuffer(0, "counterBuffer", counterBuffer);
        poissonDiskComputeShader.SetTexture(0, "heightmap", terrainData.heightmapTexture);
        poissonDiskComputeShader.SetTexture(0, "splatmap", terrainData.alphamapTextures[0]);
        //poissonDiskComputeShader.SetFloat("radius", targetRadius);
        poissonDiskComputeShader.SetFloat("terrainWidth", terrainSize.x);
        poissonDiskComputeShader.SetFloat("terrainHeight", terrainSize.z);
        poissonDiskComputeShader.SetFloat("terrainMaxHeight", terrainSize.y);
        poissonDiskComputeShader.SetInt("maxPoints", maxPoints);
        poissonDiskComputeShader.SetInt("grassLayerIndex", grassLayerIndex);

        // Dispatch the compute shader
        int threadGroups = Mathf.CeilToInt((float)maxPoints / 256);
        poissonDiskComputeShader.Dispatch(0, threadGroups, 1, 1);

        // Retrieve data from the compute shader
        RetrieveGeneratedPoints();

        // Instantiate grass at the sampled points
        InstantiateGrass();
    }

    void RetrieveGeneratedPoints()
    {
        // Get counter value
        uint[] counterValue = new uint[1];
        counterBuffer.GetData(counterValue);
        int validPointCount = (int)counterValue[0];

        // Get generated points
        Vector3[] points = new Vector3[validPointCount];
        pointsBuffer.GetData(points);
        generatedPoints.AddRange(points);

        // Clean up compute buffers
        pointsBuffer.Release();
        counterBuffer.Release();
    }

    void InstantiateGrass()
    {
        foreach (var point in generatedPoints)
        {
            Matrix4x4 transformMatrix = Matrix4x4.TRS(
                point,
                Quaternion.identity, // No rotation
                new Vector3(scaleX, scaleY, scaleZ)
            );

            instanceTransforms.Add(transformMatrix);
        }
    }

    void Update()
    {
        // Render grass using GPU instancing
        if (instanceTransforms.Count > 0)
        {
            Graphics.DrawMeshInstanced(
                grassPrefab.GetComponent<MeshFilter>().sharedMesh, // Mesh from prefab
                0,                                                 // Submesh index
                instanceMaterial,                                  // Material
                instanceTransforms                                 // Transform matrices
            );
        }
    }

    void OnDestroy()
    {
        // Release compute buffers if they haven't been released
        if (pointsBuffer != null)
            pointsBuffer.Release();
        if (counterBuffer != null)
            counterBuffer.Release();
    }

}




