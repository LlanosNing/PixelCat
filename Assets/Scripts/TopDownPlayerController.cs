using UnityEngine;

/// <summary>
/// Simple player controller for top-down/isometric games
/// Supports WASD/Arrow keys movement with optional rotation
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class TopDownPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Movement speed")]
    public float moveSpeed = 5f;

    [Tooltip("How quickly the player accelerates")]
    [Range(0.01f, 1f)]
    public float acceleration = 0.1f;

    [Header("Rotation")]
    [Tooltip("Should the player rotate to face movement direction?")]
    public bool rotateToMovement = true;

    [Tooltip("How quickly the player rotates")]
    public float rotationSpeed = 10f;

    [Header("Jump")]
    [Tooltip("Jump force")]
    public float jumpForce = 5f;

    [Tooltip("Optional: Ground checker object (child object with trigger collider at feet)")]
    public Transform groundChecker;

    [Tooltip("Layer mask for ground detection")]
    public LayerMask groundLayer = ~0;

    [Tooltip("Minimum contact points to be considered grounded")]
    public int minGroundContacts = 1;

    [Header("Input")]
    [Tooltip("Use camera-relative movement (recommended for isometric)")]
    public bool cameraRelativeMovement = true;

    private Rigidbody rb;
    private Vector3 currentVelocity = Vector3.zero;
    private Camera mainCamera;
    private int groundContactCount = 0;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Configure rigidbody for top-down movement
        rb.freezeRotation = true;
        rb.useGravity = true; // Keep gravity for ground detection

        mainCamera = Camera.main;
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
    }

    void HandleJump()
    {
        // Jump when Space is pressed and player is grounded
        if (Input.GetButtonDown("Jump") && IsGrounded())
        {
            rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
        }
    }

    void HandleMovement()
    {
        // Get input
        float horizontal = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right arrows
        float vertical = Input.GetAxisRaw("Vertical");     // W/S or Up/Down arrows

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

        if (inputDirection.magnitude > 0.1f)
        {
            // Calculate movement direction
            Vector3 moveDirection;

            if (cameraRelativeMovement && mainCamera != null)
            {
                // Camera-relative movement (better for isometric)
                Vector3 cameraForward = mainCamera.transform.forward;
                Vector3 cameraRight = mainCamera.transform.right;

                // Flatten to horizontal plane
                cameraForward.y = 0f;
                cameraRight.y = 0f;
                cameraForward.Normalize();
                cameraRight.Normalize();

                moveDirection = (cameraForward * vertical + cameraRight * horizontal).normalized;
            }
            else
            {
                // World-space movement
                moveDirection = inputDirection;
            }

            // Smooth acceleration
            Vector3 targetVelocity = moveDirection * moveSpeed;
            currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, acceleration);

            // Apply movement
            Vector3 newPosition = rb.position + currentVelocity * Time.deltaTime;
            rb.MovePosition(newPosition);

            // Rotate to face movement direction
            if (rotateToMovement && moveDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }
        }
        else
        {
            // Decelerate when no input
            currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, acceleration * 2f);
        }
    }

    // Optional: Get current movement direction (useful for animations)
    public Vector3 GetMovementDirection()
    {
        return currentVelocity.normalized;
    }

    // Optional: Get current speed (useful for animations)
    public float GetCurrentSpeed()
    {
        return currentVelocity.magnitude;
    }

    // Collision-based ground detection
    void OnCollisionEnter(Collision collision)
    {
        // Check if collision is with ground layer
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            groundContactCount++;
        }
    }

    void OnCollisionStay(Collision collision)
    {
        // Ensure we're still counting ground contacts
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            // Check if collision has contact points below the player
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f) // Surface is mostly horizontal
                {
                    return; // Still grounded
                }
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        // Check if collision was with ground layer
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            groundContactCount--;
            if (groundContactCount < 0) groundContactCount = 0;
        }
    }

    // Called by GroundChecker component (if using dedicated ground checker)
    public void OnGroundCheckerEnter(Collider other)
    {
        // Check if collision is with ground layer
        if (((1 << other.gameObject.layer) & groundLayer) != 0)
        {
            groundContactCount++;
        }
    }

    public void OnGroundCheckerExit(Collider other)
    {
        // Check if collision was with ground layer
        if (((1 << other.gameObject.layer) & groundLayer) != 0)
        {
            groundContactCount--;
            if (groundContactCount < 0) groundContactCount = 0;
        }
    }

    // Optional: Check if player is on the ground
    public bool IsGrounded()
    {
        return groundContactCount >= minGroundContacts;
    }
}
