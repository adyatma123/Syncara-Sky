using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics; // <-- NEW: Required for float3 type
using Random = UnityEngine.Random; // Resolve ambiguity with Unity.Mathematics.Random

/// <summary>
/// This behavior script makes the enemy move along a dynamically chosen spline path
/// after the initial forward movement is complete.
/// </summary>
public class SplineFollower : MonoBehaviour
{
    private EnemyController enemyController;

    [Header("Spline Route Configuration")]
    [Tooltip("List of all possible SplineContainer routes this enemy can choose from.")]
    public SplineContainer[] availableRoutes;

    [Tooltip("Speed multiplier for movement along the spline.")]
    public float splineSpeedMultiplier = 1f;

    // Runtime variables
    private SplineContainer currentRoute;
    // NEW: Reference to the GameObject of the spline route we are using, for cleanup.
    private GameObject currentRouteGameObject = null;
    private float splineProgress = 0f; // Value between 0.0 and 1.0

    void Start()
    {
        // Get the reference to the main EnemyController script on this same GameObject.
        enemyController = GetComponent<EnemyController>();
        if (enemyController == null)
        {
            Debug.LogError("SplineFollower requires an EnemyController component on the same GameObject.", this);
            enabled = false;
            return;
        }

        // 1. Choose a random spline route from the available list
        if (availableRoutes != null && availableRoutes.Length > 0)
        {
            // Use UnityEngine.Random
            int randomIndex = Random.Range(0, availableRoutes.Length);
            currentRoute = availableRoutes[randomIndex];

            // Store the chosen spline's GameObject reference for later destruction
            currentRouteGameObject = currentRoute.gameObject;

            Debug.Log($"[{gameObject.name}] selected route: {currentRoute.name}");
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] SplineFollower has no available routes defined. Disabling component.", this);
            enabled = false;
        }
    }

    void Update()
    {
        // Only start the behavior after the initial forward movement is complete AND a route is selected.
        if (enemyController.isInitialMovementComplete && currentRoute != null)
        {
            float forwardSpeed = enemyController.enemyProps.MovSpeed;
            if (forwardSpeed <= 0)
            {
                return;
            }

            // Calculate movement delta based on enemy speed and multiplier
            float moveDelta = forwardSpeed * splineSpeedMultiplier * Time.deltaTime;

            // Normalize the move delta to the total length of the spline container
            float normalizedDistance = moveDelta / currentRoute.CalculateLength();

            // Increment progress
            splineProgress += normalizedDistance;

            // Check if the end of the spline is reached (t > 1.0)
            if (splineProgress >= 1f)
            {
                // The enemy has completed the path. Destroy the enemy.
                Debug.Log($"[{gameObject.name}] reached end of spline: {currentRoute.name}. Destroying.");

                // OnDestroy will now handle the spline cleanup
                Destroy(gameObject);
                return;
            }

            // Get the position and tangent (direction) on the current spline
            // --- FIX: Use float3 for outputs from Evaluate ---
            currentRoute.Evaluate(splineProgress, out float3 position_f3, out float3 tangent_f3, out float3 up_f3);

            // Convert float3 back to Vector3 for Unity Transform usage
            Vector3 position = (Vector3)position_f3;
            Vector3 tangent = (Vector3)tangent_f3;
            Vector3 up = (Vector3)up_f3;
            // ----------------------------------------------------

            // Apply the position and rotation to the enemy
            transform.position = position;
            transform.rotation = Quaternion.LookRotation(tangent, up);
        }
    }

    /// <summary>
    /// Called when the enemy GameObject is being destroyed (either by reaching the end of the spline,
    /// being destroyed by the boundary checker, or being killed by the player).
    /// </summary>
    private void OnDestroy()
    {
        // Check if we successfully selected a route AND the route hasn't been destroyed yet
        if (currentRouteGameObject != null)
        {
            // Destroy the associated spline GameObject to keep the scene clean
            Destroy(currentRouteGameObject);
        }
    }
}
