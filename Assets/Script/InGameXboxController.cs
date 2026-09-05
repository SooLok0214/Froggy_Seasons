using UnityEngine;
using UnityEngine.InputSystem;

// Runs after the existing mobile controller so Xbox input can be added without
// changing or replacing the touch controls.
[DefaultExecutionOrder(100)]
public class InGameXboxController : MonoBehaviour
{
    [Header("InGame References")]
    public PlayerController playerController;
    public PlayerAttack playerAttack;
    public PlayerStats playerStats;
    public ThirdPersonCamera thirdPersonCamera;
    public UIManager uiManager;

    [Header("Left Stick - Movement")]
    [Range(0f, 0.95f)] public float moveDeadzone = 0.15f;
    public bool allowDpadMovement = true;

    [Header("A Button - Attack")]
    public bool holdAToRepeatAttack = true;

    [Header("Right Stick - Camera")]
    [Range(0f, 0.95f)] public float lookDeadzone = 0.15f;
    [Tooltip("Camera rotation speed in degrees per second.")]
    public float lookSensitivity = 120f;
    public bool invertLookY;

    private bool levelChoiceDirectionHeld;
    private bool levelChoiceWasOpen;

    public void Awake()
    {
        // PlayerStats already owns the InGame UI reference, so recover it here
        // if Unity loses the serialized reference after a script reload.
        if (uiManager == null && playerStats != null)
            uiManager = playerStats.uiManager;
    }

    public void Update()
    {
        Gamepad gamepad = Gamepad.current;
        if (gamepad == null)
            return;

        // Level-up selection pauses time, so D-pad buttons are handled before
        // the normal gameplay time-scale check.
        if (UpdateLevelUpSelection(gamepad) || !CanControl(gamepad))
            return;

        UpdateAttack(gamepad);
        UpdateCamera(gamepad);
    }

    public bool UpdateLevelUpSelection(Gamepad gamepad)
    {
        if (uiManager == null || !uiManager.levelUpSelectionOpen)
        {
            levelChoiceDirectionHeld = false;
            levelChoiceWasOpen = false;
            return false;
        }

        float horizontal = gamepad.dpad.ReadValue().x;
        bool directionPressed = Mathf.Abs(horizontal) >= 0.5f;

        // If a direction was already held while moving when the cards appeared,
        // require it to be released once before accepting a choice.
        if (!levelChoiceWasOpen)
        {
            levelChoiceWasOpen = true;
            levelChoiceDirectionHeld = directionPressed;
            return true;
        }

        if (!directionPressed)
        {
            levelChoiceDirectionHeld = false;
            return true;
        }

        if (levelChoiceDirectionHeld)
            return true;

        levelChoiceDirectionHeld = true;

        if (horizontal < 0f &&
            uiManager.leftChoiceButton != null)
        {
            uiManager.leftChoiceButton.onClick.Invoke();
        }
        else if (horizontal > 0f &&
            uiManager.rightChoiceButton != null)
        {
            uiManager.rightChoiceButton.onClick.Invoke();
        }

        // Block movement, attack and camera while the cards are open.
        return true;
    }

    public void FixedUpdate()
    {
        Gamepad gamepad = Gamepad.current;
        if (!CanControl(gamepad) || playerController == null ||
            !playerController.enabled || playerController.rb == null)
            return;

        Vector2 input = ApplyDeadzone(gamepad.leftStick.ReadValue(), moveDeadzone);
        if (allowDpadMovement && input.sqrMagnitude <= 0.0001f)
            input = Vector2.ClampMagnitude(gamepad.dpad.ReadValue(), 1f);

        // With no Xbox movement input, the original mobile joystick keeps control.
        if (input.sqrMagnitude <= 0.0001f)
            return;

        Vector3 cameraForward = Vector3.forward;
        Vector3 cameraRight = Vector3.right;
        if (playerController.gameCamera != null)
        {
            cameraForward = playerController.gameCamera.transform.forward;
            cameraRight = playerController.gameCamera.transform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();
        }

        Vector3 direction = cameraForward * input.y + cameraRight * input.x;
        if (direction.sqrMagnitude <= 0.0001f)
            return;

        direction.Normalize();
        playerController.direction = direction;
        playerController.isMoving = true;

        if (playerController.animator != null)
            playerController.animator.SetBool("isMoving", true);

        Rigidbody body = playerController.rb;
        body.angularVelocity = Vector3.zero;
        Vector3 velocity = body.linearVelocity;
        body.linearVelocity = new Vector3(
            direction.x * playerController.speed,
            velocity.y,
            direction.z * playerController.speed
        );

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        body.MoveRotation(Quaternion.Slerp(
            body.rotation,
            targetRotation,
            playerController.rotateSpeed * Time.fixedDeltaTime
        ));
    }

    public void UpdateAttack(Gamepad gamepad)
    {
        if (playerAttack == null || !playerAttack.enabled)
            return;

        bool attackRequested = holdAToRepeatAttack
            ? gamepad.buttonSouth.isPressed
            : gamepad.buttonSouth.wasPressedThisFrame;

        if (attackRequested)
            playerAttack.Fire();
    }

    public void UpdateCamera(Gamepad gamepad)
    {
        if (thirdPersonCamera == null || !thirdPersonCamera.enabled ||
            !thirdPersonCamera.controlsEnabled)
            return;

        Vector2 look = ApplyDeadzone(gamepad.rightStick.ReadValue(), lookDeadzone);
        if (look.sqrMagnitude <= 0.0001f)
            return;

        if (invertLookY)
            look.y = -look.y;

        thirdPersonCamera.RotateCamera(
            look * lookSensitivity * Time.unscaledDeltaTime
        );
    }

    public bool CanControl(Gamepad gamepad)
    {
        return gamepad != null && Time.timeScale > 0f &&
            (playerStats == null || !playerStats.isDead);
    }

    public Vector2 ApplyDeadzone(Vector2 input, float deadzone)
    {
        float magnitude = input.magnitude;
        if (magnitude <= deadzone)
            return Vector2.zero;

        float scaledMagnitude = Mathf.InverseLerp(
            deadzone,
            1f,
            Mathf.Min(magnitude, 1f)
        );
        return input.normalized * scaledMagnitude;
    }
}
