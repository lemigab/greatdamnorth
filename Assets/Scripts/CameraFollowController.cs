using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class CameraFollowController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private float zoomSpeed= 2f;
    [SerializeField] private float zoomLerpSpeed = 10.0f;
    [SerializeField] private float zoomMin = 5.0f;
    [SerializeField] private float zoomMax = 15.0f;
    private float currentZoom;
    private float targetZoom;
    private float zoomVelocity = 0.0f;

    private InputSystem_Actions input;
    public CinemachineCamera camera;
    private CinemachineOrbitalFollow orbitalFollow;

    private Vector2 scrollDelta;

    void Start()
    {
        input = new InputSystem_Actions();
        input.Camera.Enable();
        input.Camera.MouseZoom.performed += OnMouseScroll;

        Cursor.lockState = CursorLockMode.Locked;

        camera = GameObject.Find("PlayerCamera").GetComponent<CinemachineCamera>();
        orbitalFollow = camera.GetComponent<CinemachineOrbitalFollow>();

        targetZoom = currentZoom = orbitalFollow.Radius;
    }

    // Update is called once per frame
    void Update()
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