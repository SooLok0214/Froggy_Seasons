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
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
            MagicHeal();
    }

    public void FixedUpdate()
    {
        if (variableJoystick == null || rb == null)
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

        direction = cameraForward * variableJoystick.Vertical
            + cameraRight * variableJoystick.Horizontal;

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
