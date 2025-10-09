using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// This is the central manager for an enemy, handling its core properties,
/// universal movement (like initial forward motion and rotation), and shared methods.
/// The specific movement behavior is handled by a separate, dedicated component.
/// </summary>
public class EnemyController : MonoBehaviour
{
    [Header("Enemy Setup")]
    [Tooltip("Reference to the EnemyProps component on this GameObject.")]
    public EnemyProps enemyProps;

    // --- MODIFIED PROPERTY FOR DISTANCE-BASED INITIAL MOVEMENT ---
    [Tooltip("The World Z position where the initial forward movement stops and the main behavior takes over.")]
    public float initialMovementEndZ = 10f; // Example: Set to 10 to stop movement near the top of the screen
                                            // -------------------------------------------------------------

    [Tooltip("Time delay before destroying the GameObject after its model is destroyed.")]
    public float destroyDelay = 3f;

    // NOTE: This can be null, the behavior scripts need to get a reference to the player.
    [Tooltip("Adjustable follow speed for the 'FollowPlayer' behavior.")]
    public float followSpeed = 1f;

    [Tooltip("Reference to the enemy's visual model Renderer component for off-screen checks.")]
    public Renderer modelRenderer;
    [Tooltip("The maximum Z-axis rotation angle when the enemy is moving horizontally.")]
    public float maxZRotation = 15f;
    [Tooltip("The speed at which the Z-rotation interpolates back to zero.")]
    public float rotationSmoothSpeed = 5f;

    [Header("Boundary and State")]
    [Tooltip("The World Z position where the enemy will be destroyed (e.g., -10 for off-screen bottom).")]
    public float destroyBoundaryZ = -10f;
    // Flag to track whether the enemy has entered the player's camera view at least once.
    private bool hasBeenVisible = false;
    private bool isCurrentlyVisible = false;

    [Header("Runtime Properties (Managed by Controller)")]
    public bool isInitialMovementComplete = false;

    // References to other components on this GameObject
    private Rigidbody rb;
    private AIShoot aiShoot;
    private float lastXPosition;

    private Plane[] cameraPlanes; // Cache for frustum planes

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Validate essential components
        enemyProps = GetComponent<EnemyProps>();
        if (enemyProps == null)
        {
            Debug.LogError("EnemyProps script not found on " + gameObject.name + ". EnemyController requires EnemyProps to function.", this);
            enabled = false;
            return;
        }

        // Validate Renderer
        if (modelRenderer == null)
        {
            Debug.LogError("modelRenderer is not assigned on " + gameObject.name + ". Off-screen and shooting logic will fail.", this);
        }

        // Get optional components
        aiShoot = GetComponent<AIShoot>();

        Debug.Log($"Enemy {enemyProps.EnemyName} initialized with move speed: {enemyProps.MovSpeed}");
        lastXPosition = transform.position.x;

        // Ensure the Rigidbody is set up correctly (assuming a 3D shmup setup)
        if (rb != null)
        {
            // Freeze Z movement during behavior and rotation on X/Y
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
            rb.useGravity = false;
        }

        // Cache the camera frustum planes once if the camera exists
        if (Camera.main != null)
        {
            cameraPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        }
    }

    /// <summary>
    /// Checks if the enemy's model is currently within the main camera's view frustum.
    /// This replaces the unreliable OnBecameVisible/Invisible events for state management.
    /// </summary>
    private bool IsRendererVisible()
    {
        if (modelRenderer == null || Camera.main == null)
        {
            return false; // Cannot check visibility without a Renderer or Camera
        }

        // Re-calculate frustum planes for the current camera position
        cameraPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        // Test the bounds of the Renderer against the camera frustum planes
        return GeometryUtility.TestPlanesAABB(cameraPlanes, modelRenderer.bounds);
    }

    /// <summary>
    /// Called by the Renderer system when the object is visible by any camera.
    /// This is kept only to ensure the flag is set as a fallback.
    /// </summary>
    void OnBecameVisible()
    {
        if (!hasBeenVisible)
        {
            hasBeenVisible = true;
            Debug.Log($"[{gameObject.name}] entered the screen (OnBecameVisible fallback). Destruction eligibility enabled.");
        }
    }

    /// <summary>
    /// Called by the Renderer system when the object is no longer visible by any camera.
    /// This method is intentionally empty as visibility state is managed in Update().
    /// </summary>
    void OnBecameInvisible()
    {
        // Intentionally empty.
    }


    void Update()
    {
        // 1. Visibility State Management (Manual Check)
        CheckVisibilityAndToggleState();

        // 2. Boundary Destruction Check
        CheckForBoundaryDestruction();

        // 3. Calculate X velocity for Z-rotation
        float currentX = transform.position.x;
        float xVelocity = (currentX - lastXPosition) / Time.deltaTime;
        lastXPosition = currentX;
        RotateBasedOnXVelocity(xVelocity);

        // 4. Initial forward movement (Distance-based)
        if (!isInitialMovementComplete)
        {
            // Move forward until the target Z position is reached
            transform.position += Vector3.back * enemyProps.MovSpeed * Time.deltaTime;

            // Check for completion based on Z position
            if (transform.position.z <= initialMovementEndZ)
            {
                isInitialMovementComplete = true;
                Debug.Log($"Enemy {enemyProps.EnemyName} completed initial movement at Z={transform.position.z}.");
            }
        }
    }

    /// <summary>
    /// Checks the current visibility status and toggles the AIShoot component accordingly.
    /// </summary>
    private void CheckVisibilityAndToggleState()
    {
        bool nowVisible = IsRendererVisible();

        // If shooting logic is disabled, we don't need to run this check.
        if (aiShoot == null) return;

        // Enemy must be armed with EITHER MG or MSL to attempt activation/deactivation
        bool isArmed = enemyProps.IsArmedMG || enemyProps.IsArmedMSL;

        if (nowVisible && !isCurrentlyVisible)
        {
            // Just became visible
            isCurrentlyVisible = true;
            if (!hasBeenVisible)
            {
                hasBeenVisible = true; // Set eligibility for destruction
                Debug.Log($"[{gameObject.name}] Visibility Check: Entered view (Visible: True). Destruction eligible.");
            }

            // --- CRITICAL FIX: Only Activate if Initial Movement is also complete ---
            if (isArmed && isInitialMovementComplete)
            {
                aiShoot.Activate();
                Debug.Log($"Enemy {enemyProps.EnemyName} activated shooting (Visibility/Move Check).");
            }
        }
        else if (!nowVisible && isCurrentlyVisible)
        {
            // Just became invisible
            isCurrentlyVisible = false;
            Debug.Log($"[{gameObject.name}] Visibility Check: Left view (Visible: False).");

            // Deactivate AI Shoot
            aiShoot.Deactivate();
        }
    }


    /// <summary>
    /// Checks if the enemy has been visible and subsequently crossed the lower destruction boundary.
    /// </summary>
    private void CheckForBoundaryDestruction()
    {
        // Only check for boundary destruction if the enemy has been visible.
        if (hasBeenVisible)
        {
            // Check if the enemy has moved beyond the lower viewpoint boundary on the Z-axis.
            if (transform.position.z < destroyBoundaryZ)
            {
                Debug.Log($"[{gameObject.name}] passed Z boundary ({destroyBoundaryZ}). Destroying object.");

                // Ensure AI shoot is explicitly stopped before destruction
                if (aiShoot != null)
                {
                    aiShoot.Deactivate();
                }

                // Perform the destruction
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Rotates the enemy around its Z-axis based on its horizontal (X) velocity.
    /// </summary>
    /// <param name="xVelocity">The velocity of the enemy along the X-axis.</param>
    private void RotateBasedOnXVelocity(float xVelocity)
    {
        float targetZRotation = 0f;
        if (xVelocity > 0.01f)
        {
            targetZRotation = -maxZRotation;
        }
        else if (xVelocity < -0.01f)
        {
            targetZRotation = maxZRotation;
        }

        Quaternion currentRotation = transform.localRotation;
        float newZRotation = Mathf.LerpAngle(currentRotation.eulerAngles.z, targetZRotation, rotationSmoothSpeed * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(currentRotation.eulerAngles.x, currentRotation.eulerAngles.y, newZRotation);
    }
}
