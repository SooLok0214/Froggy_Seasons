using UnityEngine;
using System.Collections.Generic;
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

    [Header("Zoom")]
    [Tooltip("Camera distance multiplier: 1 keeps the original view, below 1 zooms in, above 1 zooms out. Can be adjusted live in Play mode.")]
    [Range(0.25f, 3f)]
    public float zoomMultiplier = 1f;

    [Header("Right-Hand Pinch Zoom")]
    public bool enablePinchZoom = true;
    [Min(0f)] public float pinchZoomSensitivity = 1f;
    [Range(0.25f, 3f)] public float minPinchZoom = 0.5f;
    [Range(0.25f, 3f)] public float maxPinchZoom = 2f;

    // Reused each frame, without allocating an array for mobile touch input.
    private readonly List<CameraTouchSample> cameraTouches = new List<CameraTouchSample>(10);
    private int secondCameraTouchId = -1;
    private float previousPinchDistance;

    public struct CameraTouchSample
    {
        public int id;
        public Vector2 position;
        public Vector2 delta;
        public UnityEngine.InputSystem.TouchPhase phase;
    }

    public float EffectiveDistance => Mathf.Max(0.1f, distance * Mathf.Clamp(zoomMultiplier, 0.25f, 3f));

    [Header("Ground Height Limit")]
    [Tooltip("Absolute world Y of the gameplay floor.")]
    public float floorWorldY = 3.05f;

    [Tooltip("Extra height above the floor to keep the camera/near clipping plane out of the ground.")]
    [Min(0f)]
    public float floorSafetyClearance = 0.15f;

    public bool preventCameraBelowFloor = true;
    public float MinimumCameraWorldY => floorWorldY + Mathf.Max(0f, floorSafetyClearance);

    [Header("Starting View")]
    public bool useConfiguredStartView = true;
    public float startYawOffset = 0f;
    public float startPitch = 20f;

    public float yaw;
    public float pitch = 20f;
    public int cameraTouchId = -1;
    public bool cameraTouching;
    public bool controlsEnabled = true;

    [Header("Crosshair")]
    public bool crosshairVisible = true;
    public Texture2D crosshairTexture;
    public Vector2 crosshairTextureSize = new Vector2(80f, 80f);

    public void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    public void OnDisable()
    {
        ResetTouchState();
        if (EnhancedTouchSupport.enabled)
            EnhancedTouchSupport.Disable();
    }

    public void Start()
    {
        if (target == null)
            return;

        yaw = target.eulerAngles.y + startYawOffset;

        if (useConfiguredStartView)
            pitch = Mathf.Clamp(startPitch, minPitch, maxPitch);
        else
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        UpdateCameraPosition(true);
    }

    public void Update()
    {
        if (!controlsEnabled || Time.timeScale <= 0f)
        {
            ResetTouchState();
            return;
        }

        TouchCameraControl();
        MouseCameraControl();
    }

    public void SetGameplayControl(bool active)
    {
        controlsEnabled = active;
        crosshairVisible = active;

        if (!active)
            ResetTouchState();
    }

    public void ResetTouchState()
    {
        cameraTouchId = -1;
        cameraTouching = false;
        secondCameraTouchId = -1;
        previousPinchDistance = 0f;
    }

    public void TouchCameraControl()
    {
        cameraTouches.Clear();
        foreach (Touch touch in Touch.activeTouches)
        {
            cameraTouches.Add(new CameraTouchSample
            {
                id = touch.touchId, position = touch.screenPosition,
                delta = touch.delta, phase = touch.phase
            });
        }
        ProcessTouchInput(cameraTouches, Screen.width);
    }

    // Separate input sampling from gesture logic so multi-finger transitions
    // can be verified without injecting input into the user's device.
    public void ProcessTouchInput(IReadOnlyList<CameraTouchSample> touches, float screenWidth)
    {
        if (!controlsEnabled || Time.timeScale <= 0f)
        {
            ResetTouchState();
            return;
        }

        int oldFirst = cameraTouchId;
        int oldSecond = secondCameraTouchId;
        if (FindActiveTouch(touches, cameraTouchId) < 0) cameraTouchId = -1;
        if (!enablePinchZoom || FindActiveTouch(touches, secondCameraTouchId) < 0)
            secondCameraTouchId = -1;
        if (cameraTouchId < 0 && secondCameraTouchId >= 0)
        {
            cameraTouchId = secondCameraTouchId;
            secondCameraTouchId = -1;
        }

        foreach (var touch in touches)
        {
            // Only fingers that START on the right may control the camera.
            // A left joystick finger remains excluded even if it crosses over.
            if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began
                || touch.position.x <= screenWidth * 0.5f
                || touch.id == cameraTouchId || touch.id == secondCameraTouchId)
                continue;
            if (cameraTouchId < 0) cameraTouchId = touch.id;
            else if (enablePinchZoom && secondCameraTouchId < 0) secondCameraTouchId = touch.id;
        }

        cameraTouching = cameraTouchId >= 0;
        bool pairChanged = oldFirst != cameraTouchId || oldSecond != secondCameraTouchId;
        if (pairChanged) previousPinchDistance = 0f;
        if (!cameraTouching) return;

        var first = touches[FindActiveTouch(touches, cameraTouchId)];
        if (secondCameraTouchId >= 0)
        {
            var second = touches[FindActiveTouch(touches, secondCameraTouchId)];
            float gap = Vector2.Distance(first.position, second.position);
            if (gap > 1f && previousPinchDistance > 1f)
            {
                float lower = Mathf.Clamp(Mathf.Min(minPinchZoom, maxPinchZoom), 0.25f, 3f);
                float upper = Mathf.Clamp(Mathf.Max(minPinchZoom, maxPinchZoom), lower, 3f);
                // Spread fingers -> smaller camera distance; pinch -> larger.
                float ratio = Mathf.Pow(previousPinchDistance / gap, Mathf.Max(0f, pinchZoomSensitivity));
                zoomMultiplier = Mathf.Clamp(zoomMultiplier * ratio, lower, upper);
            }
            previousPinchDistance = gap;
            return; // Never rotate at the same time as a two-finger zoom.
        }

        previousPinchDistance = 0f;
        // Drop the transition delta when a finger lifts, preventing a view jump.
        if (!pairChanged && first.phase == UnityEngine.InputSystem.TouchPhase.Moved)
            RotateCamera(first.delta * touchSensitivity);
    }

    private static int FindActiveTouch(IReadOnlyList<CameraTouchSample> touches, int id)
    {
        if (id < 0) return -1;
        for (int i = 0; i < touches.Count; i++)
        {
            var phase = touches[i].phase;
            if (touches[i].id == id && phase != UnityEngine.InputSystem.TouchPhase.Ended
                && phase != UnityEngine.InputSystem.TouchPhase.Canceled)
                return i;
        }
        return -1;
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
        if (target == null || Time.timeScale <= 0f)
            return;

        UpdateCameraPosition(false);
    }

    public void UpdateCameraPosition(bool immediate)
    {
        if (target == null)
            return;

        Vector3 lookPosition = target.position + Vector3.up * targetHeight;
        Quaternion cameraRotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 targetPosition = lookPosition
            + cameraRotation * Vector3.right * horizontalOffset
            - cameraRotation * Vector3.forward * EffectiveDistance;

        targetPosition = KeepAboveFloor(targetPosition);

        if (immediate)
        {
            transform.position = targetPosition;
            transform.rotation = cameraRotation;
            return;
        }

        float followAmount = smoothSpeed * Time.deltaTime;
        transform.position = KeepAboveFloor(Vector3.Lerp(transform.position, targetPosition, followAmount));
        transform.rotation = Quaternion.Slerp(transform.rotation, cameraRotation, followAmount);
    }

    public Vector3 KeepAboveFloor(Vector3 position)
    {
        if (preventCameraBelowFloor)
            position.y = Mathf.Max(position.y, MinimumCameraWorldY);
        return position;
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
        if (crosshairTexture != null)
        {
            float textureWidth = crosshairTextureSize.x * scale;
            float textureHeight = crosshairTextureSize.y * scale;
            GUI.color = crosshairColor;
            GUI.DrawTexture(
                new Rect(
                    centerX - textureWidth * 0.5f,
                    centerY - textureHeight * 0.5f,
                    textureWidth,
                    textureHeight
                ),
                crosshairTexture,
                ScaleMode.ScaleToFit,
                true
            );
            GUI.color = previousColor;
            return;
        }

        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(centerX - size * 0.5f - 1f, centerY - thickness * 0.5f - 1f, size + 2f, thickness + 2f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - thickness * 0.5f - 1f, centerY - size * 0.5f - 1f, thickness + 2f, size + 2f), Texture2D.whiteTexture);

        GUI.color = crosshairColor;
        GUI.DrawTexture(new Rect(centerX - size * 0.5f, centerY - thickness * 0.5f, size, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - thickness * 0.5f, centerY - size * 0.5f, thickness, size), Texture2D.whiteTexture);
        GUI.color = previousColor;
    }
}
