using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : BeaverController
{
    public Transform cameraTransform;

    private Vector2 moveInput;

    void FixedUpdate()
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
        }
    }

    public void OnMove(InputAction.CallbackContext context) {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnChew(InputAction.CallbackContext context) {
        if (context.performed) {
            // we can have all chew options here
            // since only one will actually succeed
            if (BreakMound()) return;
            if (BreakDam()) return;
            if (ChewLog()) return;
        }
    }

    public void OnBuild(InputAction.CallbackContext context) {
        if (context.performed) {
            // we can have all build options here
            // since only one will actually succeed
            if (BuildMound()) return;
            if (BuildLodge()) return;
            if (BuildDam()) return;
        }
    }
}