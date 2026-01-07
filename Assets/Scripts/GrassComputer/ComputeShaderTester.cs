using System.Collections.Generic;
using UnityEngine;

public class ComputeShaderTester : MonoBehaviour
{
    public ComputeShader poissonDiskComputeShader; 
    public GameObject grassPrefab; 
    private Material instanceMaterial;
    public Mesh mesh; 

    public int triangleCount;
    public int maxPoints = 1000;
    Bounds meshBounds; 
    private float targetRadius;
    [SerializeField] float grassScale = 1f;

    private ComputeBuffer trianglesBuffer;
    private ComputeBuffer pointsBuffer;
    private ComputeBuffer counterBuffer;
    private ComputeBuffer normalsBuffer;

    private List<Vector3> generatedPoints = new List<Vector3>();
    private List<Vector3> normalPoints = new List<Vector3>();

    private List<Matrix4x4> instanceTransforms = new List<Matrix4x4>(); 

    void Start()
    {

        mesh = GetComponent<MeshFilter>().mesh;
        meshBounds = GetComponent<MeshRenderer>().bounds;
        targetRadius = meshBounds.extents.magnitude * 0.001f;
        instanceMaterial = grassPrefab.GetComponent<MeshRenderer>().sharedMaterial;
        
        MeshToTriangles(mesh, out Triangle[] triangles);
        triangleCount = triangles.Length;

        // Initialize buffers
        trianglesBuffer = new ComputeBuffer(triangles.Length, sizeof(float) * 9); //(3 floats --> vertex)
        pointsBuffer = new ComputeBuffer(maxPoints, sizeof(float) * 3, ComputeBufferType.Append);
        normalsBuffer = new ComputeBuffer(maxPoints, sizeof(float) * 3, ComputeBufferType.Append);
        counterBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Raw);

        //Data to buffers
        trianglesBuffer.SetData(triangles);
        pointsBuffer.SetCounterValue(0);
        normalsBuffer.SetCounterValue(0);
        counterBuffer.SetData(new uint[] { 0 });

        poissonDiskComputeShader.SetBuffer(0, "triangles", trianglesBuffer);
        poissonDiskComputeShader.SetBuffer(0, "points", pointsBuffer);
        poissonDiskComputeShader.SetBuffer(0, "normals", normalsBuffer);
        //poissonDiskComputeShader.SetBuffer(0, "counterBuffer", counterBuffer);
        poissonDiskComputeShader.SetInt("triangleCount", triangleCount);
        poissonDiskComputeShader.SetInt("pointBufferLength", maxPoints);
        poissonDiskComputeShader.SetFloat("targetRadius", targetRadius);

        //Dispatch
        int threadGroups = Mathf.CeilToInt((float)triangleCount / 256);
        poissonDiskComputeShader.Dispatch(0, threadGroups, 1, 1);

        // Retrieve points
        RetrieveGeneratedPoints();

        //Instantiate 
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
        Vector3[] points = new Vector3[maxPoints];
        Vector3[] normals = new Vector3[maxPoints];
        pointsBuffer.GetData(points);
        normalsBuffer.GetData(normals);

        generatedPoints.AddRange(points);
        normalPoints.AddRange(normals);
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

    public void InstantiateSpheres()
    {
        Transform meshTransform = transform;

        for(int i = 0; i < generatedPoints.Count; i++)
        {

            Vector3 worldPoint = meshTransform.TransformPoint(generatedPoints[i]);
            Vector3 worldNormal = meshTransform.TransformDirection(normalPoints[i]);

            // Create a transform matrix for the instance
            Matrix4x4 transformMatrix = Matrix4x4.TRS(
                worldPoint,
                Quaternion.LookRotation(worldNormal),
                Vector3.one / grassScale 
            );

            instanceTransforms.Add(transformMatrix);
        }
    }

    private void Update()
    {
        if (instanceTransforms.Count > 0) //NOTE TO DO: Put batches
        {
            Graphics.DrawMeshInstanced(
                grassPrefab.GetComponent<MeshFilter>().sharedMesh, 
                0,                                                 
                instanceMaterial,                                  
                instanceTransforms                                 
            );
        }
    }

    public void ClearGrass()
    {
        instanceTransforms.Clear();
    }

    void OnDestroy()
    {
        // Release buffers
        trianglesBuffer.Release();
        pointsBuffer.Release();
        normalsBuffer.Release();
        counterBuffer.Release();
    }

    struct Triangle
    {
        public Vector3 v0;
        public Vector3 v1;
        public Vector3 v2;
    }
}
