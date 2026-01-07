using UnityEngine;

[ExecuteAlways]
public class PixelPerfectCameraOffset : MonoBehaviour
{
    public float pixelsPerUnit = 100f;
    private Camera mainCamera;

    void Start()
    {
        mainCamera = GetComponent<Camera>();

    }

    void LateUpdate()
    {
        if (mainCamera == null) return;

        // Calculate the pixel-perfect position
        Vector3 cameraPosition = mainCamera.transform.position;
        float unitsPerPixel = 1.0f / pixelsPerUnit;

        cameraPosition.x = Mathf.Round(cameraPosition.x / unitsPerPixel) * unitsPerPixel;
        cameraPosition.y = Mathf.Round(cameraPosition.y / unitsPerPixel) * unitsPerPixel;

        mainCamera.transform.position = cameraPosition;
    }
}
