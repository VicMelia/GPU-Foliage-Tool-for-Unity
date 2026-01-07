using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GrassSampler : MonoBehaviour
{
    public ComputeShader poissonDiskComputeShader; // Assign your compute shader
    public Terrain terrain; // Reference to the terrain
    public GameObject grassPrefab; // Grass prefab for instancing
    [SerializeField] float scaleX = 1f;
    [SerializeField] float scaleY = 1f;
    [SerializeField] float scaleZ = 1f;

    public int maxPoints = 1000; // Max candidates for sampling
    public float targetRadius = 1.0f; // Radius for sampling
    public int grassLayerIndex = 0; // Index of the grass layer in the splatmap

    private ComputeBuffer outputPointsBuffer;
    private ComputeBuffer spawnPointsBuffer;
    private ComputeBuffer gridBuffer;
    private ComputeBuffer debugBuffer;

    private List<Vector3> generatedPoints;
    private Material instanceMaterial;
    private List<Matrix4x4> instanceTransforms = new List<Matrix4x4>();

    void Start()
    {
        // Get the material of the grass prefab
        instanceMaterial = grassPrefab.GetComponent<MeshRenderer>().sharedMaterial;

        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainSize = terrainData.size;
        Vector3 terrainPosition = terrain.transform.position;

        // Grid setup
        float cellSize = targetRadius / Mathf.Sqrt(2);
        int gridWidth = Mathf.CeilToInt(terrainSize.x / cellSize);
        int gridHeight = Mathf.CeilToInt(terrainSize.z / cellSize);

        // Initial spawn points (more than one point to prevent clustering)
        List<Vector3> initialSpawnPoints = new List<Vector3>
        {
            new Vector3(terrainSize.x / 2, 0, terrainSize.z / 2),  // Center of the terrain
            new Vector3(terrainSize.x / 4, 0, terrainSize.z / 4),  // Quarter
            new Vector3(3 * terrainSize.x / 4, 0, terrainSize.z / 4), // Other quarter
            new Vector3(terrainSize.x / 4, 0, 3 * terrainSize.z / 4), // Third quarter
            new Vector3(3 * terrainSize.x / 4, 0, 3 * terrainSize.z / 4) // Last quarter
        };

        // Create buffers
        spawnPointsBuffer = new ComputeBuffer(initialSpawnPoints.Count, sizeof(float) * 3);
        outputPointsBuffer = new ComputeBuffer(maxPoints, sizeof(float) * 3, ComputeBufferType.Append);
        gridBuffer = new ComputeBuffer(gridWidth * gridHeight, sizeof(int));

        // Set buffer data
        spawnPointsBuffer.SetData(initialSpawnPoints);
        outputPointsBuffer.SetCounterValue(0); // Reset append buffer
        gridBuffer.SetData(new int[gridWidth * gridHeight]); // Initialize grid with -1

        // Set compute shader parameters
        int kernelHandle = poissonDiskComputeShader.FindKernel("CSMain");

        ShowDebugParameters(gridWidth, gridHeight, cellSize, initialSpawnPoints);
        // Initialize debug buffer
        debugBuffer = new ComputeBuffer(maxPoints, sizeof(float));
        poissonDiskComputeShader.SetBuffer(kernelHandle, "DebugBuffer", debugBuffer);

        poissonDiskComputeShader.SetBuffer(kernelHandle, "SpawnPoints", spawnPointsBuffer);
        poissonDiskComputeShader.SetBuffer(kernelHandle, "OutputPoints", outputPointsBuffer);
        poissonDiskComputeShader.SetBuffer(kernelHandle, "Grid", gridBuffer);

        poissonDiskComputeShader.SetFloat("Radius", targetRadius);
        poissonDiskComputeShader.SetFloat("Width", terrainSize.x);
        poissonDiskComputeShader.SetFloat("Height", terrainSize.z);
        poissonDiskComputeShader.SetInt("SampleCount", maxPoints);
        poissonDiskComputeShader.SetFloat("CellSize", cellSize);
        poissonDiskComputeShader.SetInt("GridWidth", gridWidth);
        poissonDiskComputeShader.SetInt("GridHeight", gridHeight);
        poissonDiskComputeShader.SetVector("TerrainPosition", terrain.transform.position);
        poissonDiskComputeShader.SetInt("OutputPointsLength", maxPoints);
        poissonDiskComputeShader.SetInt("SpawnPointsLength", initialSpawnPoints.Count);


        // Dispatch the compute shader
        int threadGroups = Mathf.CeilToInt((float)maxPoints / 256);
        poissonDiskComputeShader.Dispatch(kernelHandle, threadGroups, 1, 1);

        // Retrieve data from the compute shader
        RetreiveDebugData(initialSpawnPoints);
        RetrieveGeneratedPoints();

        // Instantiate grass at the sampled points
        InstantiateGrass();
    }

    void ShowDebugParameters(int gridHeight, int gridWidth, float cellSize, List<Vector3> initialSpawnPoints){
        // Log parameters
        Debug.Log("Max Points: " + maxPoints);
        Debug.Log("Target Radius: " + targetRadius);
        Debug.Log("Grid Width: " + gridWidth);
        Debug.Log("Grid Height: " + gridHeight);
        Debug.Log("Cell Size: " + cellSize);

        // Log initial spawn points
        Debug.Log("Initial Spawn Points Count: " + initialSpawnPoints.Count);
        foreach (var point in initialSpawnPoints)
        {
            Debug.Log("Spawn Point: " + point);
        }
    }

    void RetreiveDebugData(List<Vector3> initialSpawnPoints)
    {
        // Retrieve debug information
        float[] debugData = new float[maxPoints * 3];
        debugBuffer.GetData(debugData);

        // Check debug information
        int generatedPointsCount = 0;
        int overflowCount = 0;
        for (int i = 0; i < maxPoints; i++)
        {
            if (debugData[i] == 1.0f)
            {
                generatedPointsCount++;
            }
            else if (debugData[i] == 2.0f)
            {
                overflowCount++;
            }
        }

        Debug.Log("Generated Points Count: " + generatedPointsCount);
        Debug.Log("Overflow Count: " + overflowCount);

        // Log additional debug information
        for (int i = 0; i < initialSpawnPoints.Count; i++)
        {
            Debug.Log("Spawn Center X: " + debugData[i + maxPoints]);
            Debug.Log("Spawn Center Z: " + debugData[i + 2 * maxPoints]);
        }
    }

    void RetrieveGeneratedPoints()
    {
        

        // Create a count buffer to hold the number of points
        ComputeBuffer countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);

        // Copy the count from the append buffer into the count buffer
        ComputeBuffer.CopyCount(outputPointsBuffer, countBuffer, 0);

        // Retrieve the count
        int[] countArray = { 0 };
        countBuffer.GetData(countArray);
        int pointCount = countArray[0];

        // Check if any points were generated
        if (pointCount > 0)
        {
            // Retrieve generated points
            Vector3[] outputPoints = new Vector3[pointCount];
            outputPointsBuffer.GetData(outputPoints, 0, 0, pointCount);

            // Store points in the generatedPoints list
            generatedPoints = new List<Vector3>(outputPoints);
        }
        else
        {
            Debug.LogWarning("No points were generated by the compute shader.");
            generatedPoints = new List<Vector3>();
        }

        // Release the count buffer
        countBuffer.Release();

        // Release other buffers
        spawnPointsBuffer.Release();
        outputPointsBuffer.Release();
        gridBuffer.Release();
    }

    void InstantiateGrass()
    {
        Vector3 terrainPosition = terrain.transform.position;

        foreach (var point in generatedPoints)
        {
            Vector3 worldPosition = new Vector3(point.x, terrain.SampleHeight(point), point.z) + terrain.transform.position;

            GameObject grassInstance = Instantiate(grassPrefab, worldPosition, Quaternion.identity);

        }
    }

    void Update()
    {
        /*
        // Render grass using GPU instancing
        int batchSize = 1023; // Maximum allowed instances per batch
        for (int i = 0; i < instanceTransforms.Count; i += batchSize)
        {
            int count = Mathf.Min(batchSize, instanceTransforms.Count - i);
            Graphics.DrawMeshInstanced(
                grassPrefab.GetComponent<MeshFilter>().sharedMesh, 
                0,                                                 
                instanceMaterial,                                  
                instanceTransforms.GetRange(i, count).ToArray()    
            );
        }
        */
    }

    void OnDestroy()
    {
        // Release compute buffers if destroyed
        if (spawnPointsBuffer != null)
            spawnPointsBuffer.Release();
        if (outputPointsBuffer != null)
            outputPointsBuffer.Release();
        if (gridBuffer != null)
            gridBuffer.Release();
        
        if (debugBuffer != null)
        {
            debugBuffer.Release();
        }
    }
}
