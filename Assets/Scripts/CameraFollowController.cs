using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class CameraFollowController : MonoBehaviour
{
    private float zoomSpeed= 2f;
    private float zoomLerpSpeed = 10.0f;
    private float zoomMin = 5.0f;
    private float zoomMax = 15.0f;
    private float currentZoom;
    private float targetZoom;

    private InputSystem_Actions input;
    public CinemachineCamera cinemachineCamera;
    private CinemachineOrbitalFollow orbitalFollow;
    private NetworkObject networkObject; // To check if we're the owner

    private Vector2 scrollDelta;

    void Start()
    {
        // Get NetworkObject from parent beaver
        networkObject = GetComponentInParent<NetworkObject>();
        
        // Only set up input if we're the owner (or not networked)
        if (networkObject == null || networkObject.IsOwner)
        {
            input = new InputSystem_Actions();
            input.Camera.Enable();
            input.Camera.MouseZoom.performed += OnMouseScroll;

            Cursor.lockState = CursorLockMode.Locked;
        }

        // Get camera and orbital follow from this GameObject (the camera)
        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponent<CinemachineCamera>();
        }
        
        if (orbitalFollow == null)
        {
            orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
        }

        if (orbitalFollow != null)
        {
            targetZoom = currentZoom = orbitalFollow.Radius;
        }
    }

    void LateUpdate()
    {
        // Only process zoom if we're the owner (or not networked)
        if (networkObject != null && !networkObject.IsOwner)
        {
            return;
        }
        
        if (orbitalFollow == null) return;
        
        if (scrollDelta.y != 0)
        {
            targetZoom = Mathf.Clamp(orbitalFollow.Radius - scrollDelta.y * zoomSpeed, zoomMin, zoomMax);
            scrollDelta = Vector2.zero;
        }
        currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * zoomLerpSpeed);
        orbitalFollow.Radius = currentZoom;
    }

    private void OnMouseScroll(InputAction.CallbackContext context)
    {
        scrollDelta = context.ReadValue<Vector2>();
        //Debug.Log("Scroll delta: " + scrollDelta);
    }
    
    void OnDisable()
    {
        if (input != null)
        {
            input.Camera.MouseZoom.performed -= OnMouseScroll;
            input.Camera.Disable();
        }
    }
    
    void OnDestroy()
    {
        if (input != null)
        {
            input.Camera.MouseZoom.performed -= OnMouseScroll;
            input.Camera.Disable();
            input.Dispose();
        }
    }
}