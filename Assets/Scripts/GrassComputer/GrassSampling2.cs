using System.Collections.Generic;
using UnityEngine;

public class ComputeSampling2 : MonoBehaviour
{
    public ComputeShader poissonDiskComputeShader; // Assign your compute shader
    public GameObject grassPrefab; // Assign a Unity sphere prefab
    private Material instanceMaterial;
    public Mesh mesh; // Assign the mesh to sample points from

    Terrain terrain;

    public int triangleCount;
    public int maxPoints = 1000;
    Bounds meshBounds; //Bounding box
    private float targetRadius;
    [SerializeField] float grassScale = 1f;

    private ComputeBuffer trianglesBuffer;
    private ComputeBuffer pointsBuffer;
    private ComputeBuffer counterBuffer;

    private List<Vector3> generatedPoints = new List<Vector3>();

    private List<Matrix4x4> instanceTransforms = new List<Matrix4x4>(); // Transform matrices for GPU instancing

    void Start()
    {
        terrain = GetComponent<Terrain>();
        Vector3 terrainPosition = terrain.transform.position;
        TerrainData terrainData = terrain.terrainData;

        // Get the terrain's mesh (assumes terrain mesh is available)
        mesh = GenerateTerrainMesh(terrain.terrainData);

        instanceMaterial = grassPrefab.GetComponent<MeshRenderer>().sharedMaterial;
        // Prepare data
        MeshToTriangles(mesh, out Triangle[] triangles);
        triangleCount = triangles.Length;

        // Initialize buffers
        trianglesBuffer = new ComputeBuffer(triangles.Length, sizeof(float) * 9); // 3 floats per vertex
        pointsBuffer = new ComputeBuffer(maxPoints, sizeof(float) * 3, ComputeBufferType.Append);
        counterBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Raw);

        // Set data to buffers
        trianglesBuffer.SetData(triangles);
        pointsBuffer.SetCounterValue(0);
        counterBuffer.SetData(new uint[] { 0 });

        // Set compute shader parameters
        poissonDiskComputeShader.SetBuffer(0, "triangles", trianglesBuffer);
        poissonDiskComputeShader.SetBuffer(0, "points", pointsBuffer);
        poissonDiskComputeShader.SetBuffer(0, "counterBuffer", counterBuffer);
        poissonDiskComputeShader.SetInt("triangleCount", triangleCount);
        poissonDiskComputeShader.SetInt("pointBufferLength", maxPoints);
        poissonDiskComputeShader.SetFloat("targetRadius", targetRadius);

        // Dispatch compute shader
        int threadGroups = Mathf.CeilToInt((float)triangleCount / 256);
        poissonDiskComputeShader.Dispatch(0, threadGroups, 1, 1);

        // Retrieve data from the compute shader
        RetrieveGeneratedPoints();

        // Instantiate spheres at generated points
        InstantiateSpheres();
    }






    void MeshToTriangles(Mesh mesh, out Triangle[] triangles)
    {
        Vector3[] vertices = mesh.vertices;
        int[] indices = mesh.triangles;

        triangles = new Triangle[indices.Length / 3];
        for (int i = 0; i < indices.Length; i += 3)
        {
            triangles[i / 3] = new Triangle
            {
                v0 = vertices[indices[i]],
                v1 = vertices[indices[i + 1]],
                v2 = vertices[indices[i + 2]]
            };
        }
    }

    void RetrieveGeneratedPoints()
    {
        // Get counter value (valid point count)
        uint[] counterValue = new uint[1];
        counterBuffer.GetData(counterValue);
        int validPointCount = (int)counterValue[0];

        // Ensure the buffer has enough data
        if (validPointCount > pointsBuffer.count)
        {
            Debug.LogWarning("Requested data exceeds buffer size.");
            validPointCount = pointsBuffer.count;
        }

        // Retrieve data from the points buffer
        Vector3[] points = new Vector3[validPointCount];
        pointsBuffer.GetData(points);

        // Store the generated points
        generatedPoints.AddRange(points);
    }

    /*
    void InstantiateSpheres()
    {
        foreach (Vector3 point in generatedPoints)
        {
            // Transform the local-space point to world space
            Vector3 worldPoint = transform.TransformPoint(point);

            // Instantiate the sphere at the world position
            Instantiate(spherePrefab, worldPoint, Quaternion.identity);
        }
    }
    */

    void InstantiateSpheres()
    {
        Transform meshTransform = transform;

        foreach (var point in generatedPoints)
        {

            Vector3 worldPoint = meshTransform.TransformPoint(point);

            // Create a transform matrix for the instance
            Matrix4x4 transformMatrix = Matrix4x4.TRS(
                worldPoint,
                Quaternion.identity, // No rotation
                Vector3.one / grassScale // Scale/2
            );

            instanceTransforms.Add(transformMatrix);
        }
    }

    Mesh GenerateTerrainMesh(TerrainData terrainData)
    {
        int width = terrainData.heightmapResolution;
        int height = terrainData.heightmapResolution;

        //arrays
        Vector3[] vertices = new Vector3[width * height];
        int[] triangles = new int[(width - 1) * (height - 1) * 6];
        Vector2[] uv = new Vector2[width * height];

        //Vertices
        int vertexIndex = 0;
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                //Height
                float y = terrainData.GetHeight(x, z);

                //x,y,z vertex
                vertices[vertexIndex] = new Vector3(x, y, z);
                uv[vertexIndex] = new Vector2((float)x / (width - 1), (float)z / (height - 1)); // UV mapping
                vertexIndex++;
            }
        }

        // Triangles
        int triangleIndex = 0;
        for (int z = 0; z < height - 1; z++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                int current = z * width + x;
                int next = current + 1;
                int down = (z + 1) * width + x;
                int downNext = down + 1;

                // First triangle
                triangles[triangleIndex++] = current;
                triangles[triangleIndex++] = down;
                triangles[triangleIndex++] = next;

                // Second triangle
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = down;
                triangles[triangleIndex++] = downNext;
            }
        }

        // Create the mesh
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;

        // Recalculate normals for proper lighting
        mesh.RecalculateNormals();

        return mesh;
    }






    private void Update()
    {
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

    void OnDestroy()
    {
        // Release buffers
        trianglesBuffer.Release();
        pointsBuffer.Release();
        counterBuffer.Release();
    }

    struct Triangle
    {
        public Vector3 v0;
        public Vector3 v1;
        public Vector3 v2;
    }
}
