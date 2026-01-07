using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshSampler : MonoBehaviour
{

    Vector3[] points;
    public GameObject grassPrefab;
    Mesh mesh;
    public int sampleCount = 100;

    [SerializeField] float grassScale = 2f;

    //Poisson disk target radius
    Bounds meshBounds; //Bounding box
    float target_radius;  // 0.1% of the bounding box

    private List<Matrix4x4> instanceTransforms = new List<Matrix4x4>(); // Transform matrices for GPU instancing
    private Material instanceMaterial;

    private float totalArea;
    private float[] triangleAreas;
    private int[] pointsPerTriangle;

    int contador = 0;

    // Start is called before the first frame update
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        meshBounds = GetComponent<MeshRenderer>().bounds;
        target_radius = meshBounds.extents.magnitude * 0.001f;
        //GeneratePointsRandom();

        // Get the material of the grass prefab
        instanceMaterial = grassPrefab.GetComponent<MeshRenderer>().sharedMaterial;


        ComputeTriangleAreas();
        DistributeSamples();
        GeneratePointsRadius();


    }

    void ComputeTriangleAreas()
    {
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;
        int triangleCount = triangles.Length / 3;

        triangleAreas = new float[triangleCount];
        totalArea = 0f;

        for (int i = 0; i < triangleCount; i++)
        {
            Vector3 v0 = vertices[triangles[i * 3]];
            Vector3 v1 = vertices[triangles[i * 3 + 1]];
            Vector3 v2 = vertices[triangles[i * 3 + 2]];

            float area = Vector3.Cross(v1 - v0, v2 - v0).magnitude * 0.5f;
            triangleAreas[i] = area;
            totalArea += area;
        }
    }

    void DistributeSamples()
    {
        int triangleCount = triangleAreas.Length;
        pointsPerTriangle = new int[triangleCount];

        for (int i = 0; i < triangleCount; i++)
        {
            pointsPerTriangle[i] = Mathf.RoundToInt((triangleAreas[i] / totalArea) * sampleCount);
        }
    }

    void GeneratePointsRadius(){


        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;
        List<Vector3> points = new List<Vector3>();
        List<Vector3> activePoints = new List<Vector3>();

        int haltonIndex = 1;

        for (int i = 0; i < pointsPerTriangle.Length; i++)
        {
            int triangleIndex = i * 3;
            Vector3 v0 = vertices[triangles[triangleIndex]];
            Vector3 v1 = vertices[triangles[triangleIndex + 1]];
            Vector3 v2 = vertices[triangles[triangleIndex + 2]];

            for (int j = 0; j < pointsPerTriangle[i]; j++)
            {
                float u = Halton(haltonIndex, 2);
                float v = Halton(haltonIndex, 3);
                haltonIndex++;

                if (u + v > 1.0f)
                {
                    u = 1.0f - u;
                    v = 1.0f - v;
                }

                float w = 1.0f - u - v;
                Vector3 point = u * v0 + v * v1 + w * v2;

                points.Add(point);

                //Vector3 newPoint = GetRandomPointSphere(v0, v1, v2);
                //CheckValidPoint(newPoint, activePoints, points);
            }
        }

        //PrepareInstanceTransforms(points);

        // Prepare instance transforms for GPU instancing
        PrepareInstanceTransforms(points);

        //VisualizePoints(points);

    }

    void PrepareInstanceTransforms(List<Vector3> points){

        Transform meshTransform = transform;

        foreach (var point in points)
        {
            
            Vector3 worldPoint = meshTransform.TransformPoint(point);

            // Create a transform matrix for the instance
            Matrix4x4 transformMatrix = Matrix4x4.TRS(
                worldPoint,
                Quaternion.identity, // No rotation
                Vector3.one/grassScale // Scale/2
            );

            contador++;

            instanceTransforms.Add(transformMatrix);
        }

        Debug.Log("El contador de puntos es: " + contador);



    }

    float Halton(int index, int baseValue)
    {
        float result = 0f;
        float f = 1f / baseValue;
        int i = index;

        while (i > 0)
        {
            result += f * (i % baseValue);
            i = Mathf.FloorToInt(i / baseValue);
            f /= baseValue;
        }

        return result;
    }

    Vector3 GetRandomPointSphere(Vector3 v0, Vector3 v1, Vector3 v2){

        float u = Random.value;
        float v = Random.value;
        if(u + v >= 1.0f){
            u = 1.0f - u;
            v = 1.0f - v;
        }
        float w = 1.0f - u - v;

        return u * v0 + v * v1 + w * v2;
    
    }

    void CheckValidPoint(Vector3 newPoint, List<Vector3> activePoints, List<Vector3> points){

        bool isClose = false;
        foreach(Vector3 point in activePoints){
            if(Vector3.Distance(point, newPoint) < target_radius){
                isClose = true;
                break;
            }
        }

        if(!isClose){
            activePoints.Add(newPoint);
            points.Add(newPoint);
        }

    }

    /*
    void GeneratePointsRandom(){

        points = new Vector3[sampleCount];
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        for(int i = 0; i < sampleCount; i++){

            int triangleIndex = GetRandomIndex(triangles);
            Vector3 v0 = vertices[triangles[triangleIndex]];
            Vector3 v1 = vertices[triangles[triangleIndex+1]];
            Vector3 v2 = vertices[triangles[triangleIndex+2]];

            GetRandomPoint(v0,v1,v2, i);
            

        }

    }
    */
    /*
    void GetRandomPoint(Vector3 v0, Vector3 v1, Vector3 v2, int sample){

        float u = Random.value;
        float v = Random.value;
        if(u + v >= 1.0f){
            u = 1.0f - u;
            v = 1.0f - v;
        }

        float w = 1.0f - u - v;

        points[sample] = u * v0 + v * v1 + w * v2; //Stores the point in the array
    }
    */

    int GetRandomIndex(int[] triangles){
        return Random.Range(0, triangles.Length / 3) * 3;
    }
    

    

    void VisualizePoints(List<Vector3> points){

        Transform meshTransform = transform;

        foreach(var point in points){
            // Convert the local space point to world space
            Vector3 worldPoint = meshTransform.TransformPoint(point);

            // Instantiate the grass prefab at the world position
            Instantiate(grassPrefab, worldPoint, Quaternion.identity);
            
        }

        Debug.Log("El contador de puntos es: " + contador);

    }

    void Update()
    {

        //Debug.Log(instanceTransforms.Count);
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
