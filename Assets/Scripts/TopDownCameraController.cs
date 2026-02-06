using UnityEngine;

/// <summary>
/// Top-down orthographic camera controller with smooth following
/// Perfect for pixel art games with isometric or top-down perspective
/// </summary>
public class TopDownCameraController : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The player transform to follow")]
    public Transform target;

    [Header("Camera Settings")]
    [Tooltip("Camera angle in degrees (45 for isometric, 90 for pure top-down)")]
    [Range(0f, 90f)]
    public float cameraAngle = 45f;

    [Tooltip("Distance from target")]
    public float distance = 10f;

    [Header("Follow Settings")]
    [Tooltip("Should the camera move to follow the target? (Uncheck for fixed camera)")]
    public bool followTarget = true;

    [Tooltip("How smoothly the camera follows (lower = smoother but slower)")]
    [Range(0.01f, 1f)]
    public float followSmoothing = 0.1f;

    [Tooltip("Offset from target position (useful for centering)")]
    public Vector3 offset = Vector3.zero;

    [Header("Initial Camera Direction")]
    [Tooltip("Starting rotation angle around the player (0=North, 90=East, 180=South, 270=West)")]
    [Range(0f, 360f)]
    public float initialRotationAngle = 0f;

    [Header("Mouse Rotation")]
    [Tooltip("Enable mouse rotation")]
    public bool enableMouseRotation = true;

    [Tooltip("Enable vertical rotation (camera angle adjustment)")]
    public bool enableVerticalRotation = false;

    [Tooltip("Mouse sensitivity for rotation")]
    public float mouseSensitivity = 3f;

    [Tooltip("Smooth rotation speed")]
    [Range(0.01f, 1f)]
    public float rotationSmoothing = 0.1f;

    [Tooltip("Invert horizontal rotation")]
    public bool invertX = true;

    [Tooltip("Invert vertical rotation")]
    public bool invertY = false;

    [Tooltip("Minimum camera angle (degrees)")]
    [Range(0f, 89f)]
    public float minAngle = 10f;

    [Tooltip("Maximum camera angle (degrees)")]
    [Range(1f, 90f)]
    public float maxAngle = 80f;

    [Header("Boundaries (Optional)")]
    [Tooltip("Enable camera boundaries")]
    public bool useBoundaries = false;

    [Tooltip("Minimum X position")]
    public float minX = -50f;

    [Tooltip("Maximum X position")]
    public float maxX = 50f;

    [Tooltip("Minimum Z position")]
    public float minZ = -50f;

    [Tooltip("Maximum Z position")]
    public float maxZ = 50f;

    private Camera cam;
    private Vector3 velocity = Vector3.zero;
    private Vector3 lastTargetPosition;
    private float currentYaw = 0f;   // Horizontal rotation
    private float currentPitch = 45f; // Vertical rotation (camera angle)
    private float targetYaw = 0f;
    private float targetPitch = 45f;

    void Start()
    {
        cam = GetComponent<Camera>();

        // Ensure camera is orthographic
        if (!cam.orthographic)
        {
            cam.orthographic = true;
            Debug.Log("Camera set to orthographic mode");
        }

        // Initialize rotation angles
        currentPitch = cameraAngle;
        targetPitch = cameraAngle;
        currentYaw = initialRotationAngle;
        targetYaw = initialRotationAngle;

        // Initialize position tracking
        if (target != null)
        {
            lastTargetPosition = target.position;
            UpdateCameraPosition(true);
        }
    }

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("TopDownCameraController: No target assigned!");
            return;
        }

        HandleMouseRotation();
        UpdateCameraPosition(false);
    }

    void HandleMouseRotation()
    {
        if (!enableMouseRotation)
            return;

        // Get mouse movement directly without button check
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Apply inversion
        if (invertX) mouseX = -mouseX;
        if (invertY) mouseY = -mouseY;

        // Update target rotation
        targetYaw += mouseX;

        // Only apply vertical rotation if enabled
        if (enableVerticalRotation)
        {
            targetPitch -= mouseY; // Subtract because screen Y is inverted

            // Clamp pitch to min/max angle
            targetPitch = Mathf.Clamp(targetPitch, minAngle, maxAngle);
        }

        // Smooth rotation
        currentYaw = Mathf.Lerp(currentYaw, targetYaw, rotationSmoothing * 10f);
        currentPitch = Mathf.Lerp(currentPitch, targetPitch, rotationSmoothing * 10f);
    }

    void UpdateCameraPosition(bool instant)
    {
        // Update focus point if following is enabled
        if (followTarget && target != null)
        {
            lastTargetPosition = target.position;
        }
        // Calculate focus point with offset
        Vector3 focusPoint = lastTargetPosition + offset;

        // Calculate camera position based on rotation angles and distance
        float pitchRad = currentPitch * Mathf.Deg2Rad;
        float yawRad = currentYaw * Mathf.Deg2Rad;

        float height = distance * Mathf.Sin(pitchRad);
        float horizontalDistance = distance * Mathf.Cos(pitchRad);

        // Calculate position using yaw for horizontal rotation
        Vector3 desiredPosition = new Vector3(
            focusPoint.x + horizontalDistance * Mathf.Sin(yawRad),
            focusPoint.y + height,
            focusPoint.z - horizontalDistance * Mathf.Cos(yawRad)
        );

        // Apply boundaries if enabled
        if (useBoundaries)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.z = Mathf.Clamp(desiredPosition.z, minZ, maxZ);
        }

        // Smooth follow or instant snap
        if (instant)
        {
            transform.position = desiredPosition;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref velocity,
                followSmoothing
            );
        }

        // Always look at focus point
        transform.LookAt(focusPoint);
    }

    // Helper method to set camera angle at runtime
    public void SetCameraAngle(float angle)
    {
        cameraAngle = Mathf.Clamp(angle, 0f, 90f);
    }

    // Helper method to set orthographic size
    public void SetOrthographicSize(float size)
    {
        if (cam != null)
        {
            cam.orthographicSize = size;
        }
    }
}