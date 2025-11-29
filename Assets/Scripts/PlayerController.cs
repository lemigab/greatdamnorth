using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Cinemachine;

public class PlayerController : BeaverController
{
    public Transform cameraTransform;

    private Vector2 moveInput;
    private PlayerInput playerInput;
    private CinemachineCamera cinemachineCamera;
    private CameraFollowController cameraFollowController;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Get PlayerInput component
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
        
        // Find the beaver's camera (child object)
        if (cinemachineCamera == null)
        {
            cinemachineCamera = GetComponentInChildren<CinemachineCamera>();
        }
        
        // Find CameraFollowController (should be on the camera GameObject)
        if (cameraFollowController == null && cinemachineCamera != null)
        {
            cameraFollowController = cinemachineCamera.GetComponent<CameraFollowController>();
        }
        
        // Only enable input and camera for the owner
        if (IsOwner)
        {
            // Enable PlayerInput
            if (playerInput != null)
            {
                playerInput.enabled = true;
                Debug.Log($"[{gameObject.name}] OnNetworkSpawn - Owner: PlayerInput ENABLED");
            }
            
            // Enable and set up camera
            if (cinemachineCamera != null)
            {
                cinemachineCamera.enabled = true;
                cinemachineCamera.Priority = 10; // High priority for owner's camera
                
                // Set cameraTransform for movement calculations
                if (cameraTransform == null)
                {
                    cameraTransform = cinemachineCamera.transform;
                }
                
                // Enable camera follow controller
                if (cameraFollowController != null)
                {
                    cameraFollowController.enabled = true;
                }
                
                Debug.Log($"[{gameObject.name}] OnNetworkSpawn - Owner: Camera ENABLED");
            }
        }
        else
        {
            // Disable PlayerInput for non-owners
            if (playerInput != null)
            {
                playerInput.enabled = false;
                Debug.Log($"[{gameObject.name}] OnNetworkSpawn - Non-owner: PlayerInput DISABLED");
            }
            
            // Disable camera for non-owners
            if (cinemachineCamera != null)
            {
                cinemachineCamera.enabled = false;
                cinemachineCamera.Priority = 0; // Low priority
                
                // Disable camera follow controller
                if (cameraFollowController != null)
                {
                    cameraFollowController.enabled = false;
                }
                
                Debug.Log($"[{gameObject.name}] OnNetworkSpawn - Non-owner: Camera DISABLED");
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void MoveRpc(Vector3 direction) {
        //Debug.Log("PlayerController MoveRpc: direction: " + direction);
        base.Move(direction);
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;
        //Debug.Log("PlayerController FixedUpdate: moveInput: " + moveInput);
        if (moveInput.x != 0 || moveInput.y != 0) {
            //Debug.Log("PlayerController FixedUpdate: we are moving");
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();
            Vector3 direction = forward * moveInput.y + right * moveInput.x;
            Vector3 targetDirection = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0) * direction;
            //Debug.Log("PlayerController FixedUpdate: direction: " + direction);
            MoveRpc(direction);
        }
    }

    public void OnMove(InputAction.CallbackContext context) {
        // Only process input if we're the owner
        if (!IsOwner) {
            //Debug.Log($"PlayerController OnMove: NOT OWNER - IsOwner={IsOwner}, OwnerClientId={OwnerClientId}, LocalClientId={NetworkManager.Singleton?.LocalClientId}");
            return;
        }
        
        if (context.canceled) {
            moveInput = Vector2.zero;
           // Debug.Log($"PlayerController OnMove: CANCELED - setting to zero (IsOwner={IsOwner})");
        } else {
            moveInput = context.ReadValue<Vector2>();
           // Debug.Log($"PlayerController OnMove: {context.phase} - moveInput: {moveInput} (IsOwner={IsOwner}, gameObject={gameObject.name})");
        }
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