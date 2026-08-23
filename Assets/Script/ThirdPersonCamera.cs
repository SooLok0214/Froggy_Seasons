using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;

    public float distance = 5f;
    public float targetHeight = 1f;
    public float touchSensitivity = 0.12f;
    public float mouseSensitivity = 0.08f;
    public float smoothSpeed = 12f;
    public float minPitch = 10f;
    public float maxPitch = 65f;

    public float yaw;
    public float pitch = 20f;
    public int cameraTouchId = -1;
    public bool cameraTouching;

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
        TouchCameraControl();
        MouseCameraControl();
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
}
