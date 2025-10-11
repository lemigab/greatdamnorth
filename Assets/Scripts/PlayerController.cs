using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private InputSystem_Actions input = null;
    private BeaverController beaver = null;

    void Awake()
    {
        input = new InputSystem_Actions();
    }
    void Start()
    {
        //input = new InputSystem_Actions();
        beaver = GetComponent<BeaverController>();
    }
    void Update()
    {
        Vector2 moveInput = input.Player.Move.ReadValue<Vector2>();
        if (moveInput.x != 0 || moveInput.y != 0) {
            Vector3 direction = new Vector3(moveInput.x, 0, moveInput.y);
            Vector3 targetDirection = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0) * direction;
            beaver.Move(targetDirection);
        }

        if (input.Player.Chew.IsPressed()) {
            beaver.Chew();
        }

        if (input.Player.Interact.IsPressed()) {
            beaver.BuildDam();
        }
        if (input.Player.BreakDam.IsPressed()) {
            beaver.BreakDam();
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
}
