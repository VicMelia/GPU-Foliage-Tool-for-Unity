using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainSampler : MonoBehaviour
{
    public Terrain terrain; // Reference to the terrain
    public GameObject grassPrefab;
    public int sampleCount = 1000;
    public int grassLayerIndex = 0; // Index of the GrassLayer in the terrain's texture layers

    [SerializeField] float scaleX = 1f;
    [SerializeField] float scaleY = 1f;
    [SerializeField] float scaleZ = 1f;
    [SerializeField] float targetRadius = 1f; // Target radius for Poisson disk sampling

    private List<Matrix4x4> instanceTransforms = new List<Matrix4x4>(); // Transform matrices for GPU instancing
    private Material instanceMaterial;

    int contador = 0;

    void Start()
    {
        // Get the material of the grass prefab
        instanceMaterial = grassPrefab.GetComponent<MeshRenderer>().sharedMaterial;

        // Generate points
        GeneratePointsOnTerrain();
    }

    void GeneratePointsOnTerrain()
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 terrainPosition = terrain.transform.position;
        List<Vector3> points = PoissonDiskSampling.GeneratePoints(targetRadius, terrainData.size.x, terrainData.size.z, sampleCount);

        List<Vector3> validPoints = new List<Vector3>();

        foreach (var point in points)
        {
            // Get the height at the point
            float y = terrainData.GetHeight((int)(point.x / terrainData.size.x * terrainData.heightmapResolution), 
                                             (int)(point.z / terrainData.size.z * terrainData.heightmapResolution));

            Vector3 worldPoint = new Vector3(point.x, y, point.z) + terrainPosition;

            //validPoints.Add(worldPoint);

            
            // Check if this point is on the grass layer
            if (IsOnGrassLayer(worldPoint))
            {
                validPoints.Add(worldPoint);
            }
            
        }

        // Prepare instance transforms for GPU instancing
        PrepareInstanceTransforms(validPoints);
        //VisualizePoints(validPoints);
    }

    void VisualizePoints(List<Vector3> points)
    {

        Transform meshTransform = transform;

        foreach (var point in points)
        {
            

            // Instantiate the grass prefab at the world position
            Instantiate(grassPrefab, point, Quaternion.identity);
            contador++;

        }

        Debug.Log("El contador de puntos es: " + contador);

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

        // Get splatmap data
        float[,,] splatmaps = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);

        // Check if GrassLayer is dominant
        return splatmaps[mapZ, mapX, grassLayerIndex] > 0.5f;
    }


    void PrepareInstanceTransforms(List<Vector3> points)
    {
        foreach (var point in points)
        {
            // Create a transform matrix for the instance
            Matrix4x4 transformMatrix = Matrix4x4.TRS(
                point,
                Quaternion.identity, // No rotation
                new Vector3(scaleX, scaleY, scaleZ)
            );
            contador++;
            instanceTransforms.Add(transformMatrix);
        }
        Debug.Log("El contador de puntos PARA EL TERRENO es: " + contador);
    }

    void Update()
    {
        // Render grass using GPU instancing
        if (instanceTransforms.Count > 0)
        {
            Graphics.DrawMeshInstanced(
                grassPrefab.GetComponent<MeshFilter>().sharedMesh, // The mesh of the prefab
                0,                                                 // Submesh index
                instanceMaterial,                                  // Shared material
                instanceTransforms                                 // List of instance transforms
            );
        }
    }
}

public static class PoissonDiskSampling
{
    public static List<Vector3> GeneratePoints(float radius, float width, float height, int sampleCount)
    {
        float cellSize = radius / Mathf.Sqrt(2);
        int[,] grid = new int[Mathf.CeilToInt(width / cellSize), Mathf.CeilToInt(height / cellSize)];
        List<Vector3> points = new List<Vector3>();
        List<Vector3> spawnPoints = new List<Vector3>();

        spawnPoints.Add(new Vector3(width / 2, 0, height / 2));

        while (spawnPoints.Count > 0)
        {
            int spawnIndex = Random.Range(0, spawnPoints.Count);
            Vector3 spawnCenter = spawnPoints[spawnIndex];
            bool candidateAccepted = false;

            for (int i = 0; i < sampleCount; i++)
            {
                float angle = Random.value * Mathf.PI * 2;
                Vector3 dir = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                Vector3 candidate = spawnCenter + dir * Random.Range(radius, 2 * radius);

                if (IsValid(candidate, width, height, cellSize, radius, points, grid))
                {
                    points.Add(candidate);
                    spawnPoints.Add(candidate);
                    grid[(int)(candidate.x / cellSize), (int)(candidate.z / cellSize)] = points.Count;
                    candidateAccepted = true;
                    break;
                }
            }

            if (!candidateAccepted)
            {
                spawnPoints.RemoveAt(spawnIndex);
            }
        }

        return points;
    }

    static bool IsValid(Vector3 candidate, float width, float height, float cellSize, float radius, List<Vector3> points, int[,] grid)
    {
        if (candidate.x >= 0 && candidate.x < width && candidate.z >= 0 && candidate.z < height)
        {
            int cellX = (int)(candidate.x / cellSize);
            int cellZ = (int)(candidate.z / cellSize);
            int searchStartX = Mathf.Max(0, cellX - 2);
            int searchEndX = Mathf.Min(cellX + 2, grid.GetLength(0) - 1);
            int searchStartZ = Mathf.Max(0, cellZ - 2);
            int searchEndZ = Mathf.Min(cellZ + 2, grid.GetLength(1) - 1);

            for (int x = searchStartX; x <= searchEndX; x++)
            {
                for (int z = searchStartZ; z <= searchEndZ; z++)
                {
                    int pointIndex = grid[x, z] - 1;
                    if (pointIndex != -1)
                    {
                        float sqrDst = (candidate - points[pointIndex]).sqrMagnitude;
                        if (sqrDst < radius * radius)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        return false;
    }


}