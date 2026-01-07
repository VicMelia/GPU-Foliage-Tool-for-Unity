using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthColorFadeEffect : MonoBehaviour
{
    public Material fullscreenMaterial;
    public Transform player;
    public Camera mainCamera;

    private void Awake()
    {
        player = GameObject.Find("Player").transform;
    }

    void Update()
    {
        Shader.SetGlobalFloat("_PlayerY", player.position.y);//Vector3 viewportPos = mainCamera.WorldToViewportPoint(player.position);
        Camera cam = Camera.main;
        fullscreenMaterial.SetMatrix("_CameraInverseProjection", cam.projectionMatrix.inverse);
        fullscreenMaterial.SetMatrix("_CameraInverseView", cam.cameraToWorldMatrix);
    }
}
