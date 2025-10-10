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
    private bool weaponsActivated = false; // NEW FLAG to prevent redundant Activate() calls

    // References to other components on this GameObject
    private Rigidbody rb;
    private EnemyMG mgShoot; // FIX: Component name updated
    private EnemyMSL mslShoot; // FIX: Component name updated
    private float lastXPosition;

    private Plane[] cameraPlanes; // Cache for frustum planes

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Validate essential components
        enemyProps = GetComponent<EnemyProps>();
        if (enemyProps == null)
        {
            Debug.LogError("FATAL ERROR: EnemyProps script not found on " + gameObject.name + ". EnemyController requires EnemyProps to function.", this);
            enabled = false;
            return;
        }

        // Validate Renderer
        if (modelRenderer == null)
        {
            Debug.LogError("CONFIG ERROR: modelRenderer is not assigned on " + gameObject.name + ". Off-screen and shooting logic will fail.", this);
        }
        else
        {
            Debug.Log($"CONFIG CHECK: Model Renderer assigned on {gameObject.name}.");
        }

        // FIX: Get references to the new split weapon components
        mgShoot = GetComponent<EnemyMG>();
        mslShoot = GetComponent<EnemyMSL>();

        Debug.Log($"WEAPON CHECK: MG Component found: {mgShoot != null}.");
        Debug.Log($"WEAPON CHECK: MSL Component found: {mslShoot != null}.");
        Debug.Log($"WEAPON DATA CHECK: Is Armed MG: {enemyProps.IsArmedMG}. Is Armed MSL: {enemyProps.IsArmedMSL}.");

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
    /// </summary>
    private bool IsRendererVisible()
    {
        if (modelRenderer == null || Camera.main == null)
        {
            return false;
        }

        cameraPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        return GeometryUtility.TestPlanesAABB(cameraPlanes, modelRenderer.bounds);
    }

    void OnBecameVisible()
    {
        if (!hasBeenVisible)
        {
            hasBeenVisible = true;
            Debug.Log($"[{gameObject.name}] entered the screen (OnBecameVisible fallback). Destruction eligibility enabled.");
        }
    }

    void OnBecameInvisible()
    {
        // Intentionally empty. State management is in Update().
    }


    void Update()
    {
        // 1. Visibility State Management (Manual Check)
        CheckVisibilityAndToggleState();

        // 2. Weapon Activation Management (Runs every frame until activated)
        TryActivateWeapons();

        // 3. Boundary Destruction Check
        CheckForBoundaryDestruction();

        // 4. Calculate X velocity for Z-rotation
        float currentX = transform.position.x;
        float xVelocity = (currentX - lastXPosition) / Time.deltaTime;
        lastXPosition = currentX;
        RotateBasedOnXVelocity(xVelocity);

        // 5. Initial forward movement (Distance-based)
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
    /// Attempts to activate weapons once the enemy is ready (visible AND initial movement complete).
    /// </summary>
    private void TryActivateWeapons()
    {
        if (weaponsActivated) return; // Already activated, exit early

        bool isArmed = enemyProps.IsArmedMG || enemyProps.IsArmedMSL;

        // Check the combined condition: MUST be visible AND initial move must be complete
        if (isArmed && isInitialMovementComplete && isCurrentlyVisible)
        {
            weaponsActivated = true; // Set flag to prevent future calls

            Debug.Log($"--- WEAPON ACTIVATION SUCCESS ---");
            Debug.Log($"Enemy {enemyProps.EnemyName} activated shooting (Move and Visibility Complete).");

            if (mgShoot != null) mgShoot.Activate();
            if (mslShoot != null) mslShoot.Activate();
        }
        else
        {
            // Log block only runs if activation is still possible but one condition is missing
            if (isArmed && !weaponsActivated)
            {
                Debug.Log($"ACTIVATE BLOCKED: Move Complete: {isInitialMovementComplete}, Visible: {isCurrentlyVisible}. Waiting for conditions...");
            }
        }
    }


    /// <summary>
    /// Checks the current visibility status and toggles the weapon components accordingly.
    /// </summary>
    private void CheckVisibilityAndToggleState()
    {
        bool nowVisible = IsRendererVisible();
        bool isArmed = enemyProps.IsArmedMG || enemyProps.IsArmedMSL;

        if (nowVisible && !isCurrentlyVisible)
        {
            // Just became visible (or re-visible)
            isCurrentlyVisible = true;
            if (!hasBeenVisible)
            {
                hasBeenVisible = true; // Set eligibility for destruction
                Debug.Log($"[{gameObject.name}] Visibility Check: Entered view (Visible: True). Destruction eligible.");
            }
        }
        else if (!nowVisible && isCurrentlyVisible)
        {
            // Just became invisible
            isCurrentlyVisible = false;
            Debug.Log($"[{gameObject.name}] Visibility Check: Left view (Visible: False).");

            // --- CRITICAL FIX: Deactivate both weapon systems and reset flag ---
            if (mgShoot != null) mgShoot.Deactivate();
            if (mslShoot != null) mslShoot.Deactivate();

            weaponsActivated = false; // Enemy is now deactivated and must re-trigger Activate() when visible again
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

                // Ensure ALL weapon scripts are explicitly stopped before destruction
                if (mgShoot != null) mgShoot.Deactivate();
                if (mslShoot != null) mslShoot.Deactivate();

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
