using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

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

    private Vector2 scrollDelta;

    void Start()
    {
        input = new InputSystem_Actions();
        input.Camera.Enable();
        input.Camera.MouseZoom.performed += OnMouseScroll;

        Cursor.lockState = CursorLockMode.Locked;

        cinemachineCamera = GameObject.Find("PlayerCamera").GetComponent<CinemachineCamera>();
        orbitalFollow = cinemachineCamera.GetComponent<CinemachineOrbitalFollow>();

        targetZoom = currentZoom = orbitalFollow.Radius;
    }

    void LateUpdate()
    {
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
        Debug.Log("Scroll delta: " + scrollDelta);
    }
}