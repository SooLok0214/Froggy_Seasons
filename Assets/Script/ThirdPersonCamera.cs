using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;

    public float distance = 5f;
    public float targetHeight = 1f;
    public float horizontalOffset = 0f;
    public float touchSensitivity = 0.12f;
    public float mouseSensitivity = 0.08f;
    public float smoothSpeed = 12f;
    public float minPitch = 10f;
    public float maxPitch = 65f;

    public float yaw;
    public float pitch = 20f;
    public int cameraTouchId = -1;
    public bool cameraTouching;
    public bool controlsEnabled = true;
    public bool crosshairVisible = true;

    public void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    public void OnDisable()
    {
        if (EnhancedTouchSupport.enabled)
            EnhancedTouchSupport.Disable();
    }

    public void Start()
    {
        if (target != null)
            yaw = target.eulerAngles.y;
    }

    public void Update()
    {
        if (!controlsEnabled)
            return;

        TouchCameraControl();
        MouseCameraControl();
    }

    public void SetGameplayControl(bool active)
    {
        controlsEnabled = active;
        crosshairVisible = active;

        if (!active)
        {
            cameraTouchId = -1;
            cameraTouching = false;
        }
    }

    public void TouchCameraControl()
    {
        foreach (Touch touch in Touch.activeTouches)
        {
            if (!cameraTouching
                && touch.phase == UnityEngine.InputSystem.TouchPhase.Began
                && touch.screenPosition.x > Screen.width * 0.5f)
            {
                cameraTouchId = touch.touchId;
                cameraTouching = true;
            }

            if (!cameraTouching || touch.touchId != cameraTouchId)
                continue;

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
                RotateCamera(touch.delta * touchSensitivity);

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended
                || touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                cameraTouchId = -1;
                cameraTouching = false;
            }
        }
    }

    public void MouseCameraControl()
    {
        if (Mouse.current == null || cameraTouching)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.isPressed
            && mousePosition.x > Screen.width * 0.5f)
        {
            RotateCamera(Mouse.current.delta.ReadValue() * mouseSensitivity);
        }
    }

    public void RotateCamera(Vector2 delta)
    {
        yaw += delta.x;
        pitch -= delta.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    public void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 lookPosition = target.position + Vector3.up * targetHeight;
        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 targetPosition = lookPosition
            + cameraRotation * Vector3.right * horizontalOffset
            - cameraRotation * Vector3.forward * distance;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.unscaledDeltaTime
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            cameraRotation,
            smoothSpeed * Time.unscaledDeltaTime
        );
    }

    public Color crosshairColor = Color.white;
    public float crosshairSize = 20f;
    public float crosshairThickness = 2f;

    public void OnGUI()
    {
        if (!crosshairVisible)
            return;

        float scale = Mathf.Max(1f, Screen.height / 1080f);
        float size = crosshairSize * scale;
        float thickness = crosshairThickness * scale;
        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;

        Color previousColor = GUI.color;
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(centerX - size * 0.5f - 1f, centerY - thickness * 0.5f - 1f, size + 2f, thickness + 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - thickness * 0.5f - 1f, centerY - size * 0.5f - 1f, thickness + 2f, size + 2f), Texture2D.whiteTexture);

        GUI.color = crosshairColor;
        GUI.DrawTexture(new Rect(centerX - size * 0.5f, centerY - thickness * 0.5f, size, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - thickness * 0.5f, centerY - size * 0.5f, thickness, size), Texture2D.whiteTexture);
        GUI.color = previousColor;
    }
}