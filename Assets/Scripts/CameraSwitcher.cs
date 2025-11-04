using UnityEngine;
using Unity.Cinemachine;
using System;

public class CameraSwitcher : MonoBehaviour
{
    public CinemachineCamera[] cameras;
    

    public CinemachineCamera startCamera;
    private CinemachineCamera currentCamera;

    private InputSystem_Actions input;

    void Awake()
    {
        input = new InputSystem_Actions();
    }

    void Start()
    {
        SwitchCamera(startCamera);
    }

    void Update()
    {
        if (input.Player.Previous.IsPressed())
        {
            SwitchCamera(startCamera);
        }
        if (input.Player.Next.IsPressed())
        {
            SwitchToNextCamera();
        }
    }

    void OnEnable()
    {
        input.Player.Enable();
    }

    void OnDisable()
    {
        input.Player.Disable();
    }

    public void SwitchCamera(CinemachineCamera camera)
    {
        currentCamera = camera;
        currentCamera.Priority = 10;
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != currentCamera)
            {
                cameras[i].Priority = 5;
            }
        }
    }

    public void SwitchToNextCamera()
    {
        int currentIndex = System.Array.IndexOf(cameras, currentCamera);
        int nextIndex = (currentIndex + 1) % cameras.Length;
        SwitchCamera(cameras[nextIndex]);
    }
}