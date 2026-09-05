using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float speed = 8f;
    public float rotateSpeed = 12f;

    public VariableJoystick variableJoystick;
    public Rigidbody rb;
    public Animator animator;
    public GameObject gameCamera;

    public Vector3 direction;
    public bool isMoving;

    [Header("PC Controls")]
    [Tooltip("Use W/A/S/D to move relative to the camera. The mobile joystick keeps priority while dragged.")]
    public bool enableWASD = true;

    private Vector2 keyboardMovement;

    public void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (rb != null)
            rb.angularDamping = 10f;

        if (animator != null)
            animator.applyRootMotion = false;
    }

    public void Update()
    {
        keyboardMovement = Vector2.zero;
        Keyboard keyboard = Keyboard.current;
        if (enableWASD && keyboard != null)
        {
            keyboardMovement = new Vector2(
                (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
        }

        if (keyboard != null && keyboard.hKey.wasPressedThisFrame)
            MagicHeal();
    }

    private void OnDisable()
    {
        keyboardMovement = Vector2.zero;
    }

    public void FixedUpdate()
    {
        if (rb == null || Time.timeScale <= 0f)
            return;

        Vector2 joystickInput = variableJoystick != null
            ? new Vector2(variableJoystick.Horizontal, variableJoystick.Vertical)
            : Vector2.zero;
        Move(ResolveMovementInput(joystickInput, keyboardMovement, enableWASD));
    }

    // Keep input selection separate so it can be checked without injecting keys
    // into the Editor or changing the connected gamepad/mobile devices.
    public static Vector2 ResolveMovementInput(Vector2 joystick, Vector2 keyboard, bool wasdEnabled)
    {
        if (joystick.sqrMagnitude > 0.01f || !wasdEnabled)
            return Vector2.ClampMagnitude(joystick, 1f);

        return Vector2.ClampMagnitude(keyboard, 1f);
    }

    public void Move(Vector2 input)
    {
        if (rb == null || Time.timeScale <= 0f)
            return;

        // Movement controls the facing direction. Physics collisions must not leave
        // angular momentum behind after the joystick returns to the centre.
        rb.angularVelocity = Vector3.zero;

        Vector3 cameraForward = Vector3.forward;
        Vector3 cameraRight = Vector3.right;

        if (gameCamera != null)
        {
            cameraForward = gameCamera.transform.forward;
            cameraRight = gameCamera.transform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();
        }

        direction = cameraForward * input.y + cameraRight * input.x;

        isMoving = direction.magnitude > 0.1f;

        if (animator != null)
            animator.SetBool("isMoving", isMoving);

        Vector3 velocity = rb.linearVelocity;

        if (isMoving)
        {
            direction.Normalize();
            rb.linearVelocity = new Vector3(
                direction.x * speed,
                velocity.y,
                direction.z * speed
            );

            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                rotateSpeed * Time.fixedDeltaTime
            ));
        }
        else
        {
            rb.linearVelocity = new Vector3(0, velocity.y, 0);
        }
    }

    public void MagicHeal()
    {
        if (animator != null)
            animator.SetTrigger("MagicHeal");
    }
}
