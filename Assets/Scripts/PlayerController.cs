using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : BeaverController
{
    private InputSystem_Actions input = null;
    //private BeaverController beaver = null;
    public Transform cameraTransform;
    public bool shouldFaceMoveDirection = false;

    private Vector2 moveInput;

    private CharacterController characterController;

    void Awake()
    {
        input = new InputSystem_Actions();
    }

    void Update()
    {
        if (moveInput.x != 0 || moveInput.y != 0) {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();
            Vector3 direction = forward * moveInput.y + right * moveInput.x;
            Vector3 targetDirection = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0) * direction;
            base.Move(direction);
            if (shouldFaceMoveDirection && direction.magnitude > 0.001f) {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            }
        }
    }

    void OnEnable()
    {
        input.Player.Enable();
        input.CameraControls.Enable();
    }

    void OnDisable()
    {
        input.Player.Disable();
        input.CameraControls.Disable();
    }

    public void OnMove(InputAction.CallbackContext context) {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnChew(InputAction.CallbackContext context) {
        if (context.performed) {
            Debug.Log("Chew");
            base.Chew();
        }
    }

    public void OnInteract(InputAction.CallbackContext context) {
        if (context.performed) {
            Debug.Log("InteractWithDam");
            base.InteractWithDam();
        }
    }

    public void OnBuildDam(InputAction.CallbackContext context) {
        if (context.performed) {
            Debug.Log("BuildDam");
            base.BuildDam();
        }
        Debug.Log("BuildDam");
    }

    public void OnBreakDam(InputAction.CallbackContext context) {
        if (context.performed) {
           // Debug.Log("BreakDam");
          //  base.BreakDam();
        }
    }
}
