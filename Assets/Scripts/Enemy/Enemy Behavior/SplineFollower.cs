using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics; // <-- Required for float3 type
using Random = UnityEngine.Random; // Resolve ambiguity with Unity.Mathematics.Random

// NEW: Define AlignAxis based on common Unity component practices
public enum AlignAxis
{
    XAxis,
    YAxis,
    ZAxis,
    NegativeXAxis,
    NegativeYAxis,
    NegativeZAxis
}

// NEW: Define AlignmentMode based on SplineAnimate.cs
public enum AlignmentMode
{
    /// <summary> No aligment is done and object's rotation is unaffected. </summary>
    [InspectorName("None")]
    None,
    /// <summary> The object's forward and up axes align to the spline's tangent and up vectors. </summary>
    [InspectorName("Spline Element")]
    SplineElement,
    /// <summary> The object's forward and up axes align to the spline tranform's z-axis and y-axis. (Not fully implemented in this simple script) </summary>
    [InspectorName("Spline Object")]
    SplineObject,
    /// <summary> The object's forward and up axes align to to the world's z-axis and y-axis. (Not supported by the Spline.Evaluate methods used here, removing for simplicity) </summary>
    // [InspectorName("World Space")]
    // World // Removing World mode for simplicity as Spline.Evaluate gives local Up vector
}

/// <summary>
/// This behavior script makes the enemy move along a dynamically chosen spline path
/// after the initial forward movement is complete.
/// </summary>
public class SplineFollower : MonoBehaviour, IEnemyBehavior // <-- IMPLEMENT IEnemyBehavior
{
    private EnemyController enemyController;

    [Header("Spline Route Configuration")]
    [Tooltip("List of all possible SplineContainer routes this enemy can choose from.")]
    public SplineContainer[] availableRoutes;

    [Tooltip("Speed multiplier for movement along the spline.")]
    public float splineSpeedMultiplier = 1f;

    [Header("Alignment Configuration")]
    [Tooltip("The coordinate space that the GameObject's up and forward axes align to. Spline Object is not fully supported.")]
    public AlignmentMode alignmentMode = AlignmentMode.SplineElement; // NEW

    [Tooltip("Which axis of the GameObject is treated as the forward axis.")]
    public AlignAxis objectForwardAxis = AlignAxis.ZAxis; // NEW

    [Tooltip("Which axis of the GameObject is treated as the up axis.")]
    public AlignAxis objectUpAxis = AlignAxis.YAxis; // NEW

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

        // NEW: Ensure the forward and up axes are not the same (copied from SplineAnimate internal logic)
        EnsureDifferentAxes(ref objectForwardAxis, ref objectUpAxis);
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
            currentRoute.Evaluate(splineProgress, out float3 position_f3, out float3 tangent_f3, out float3 up_f3);

            // Convert float3 back to Vector3 for Unity Transform usage
            Vector3 position = (Vector3)position_f3;
            Vector3 tangent = (Vector3)tangent_f3;
            Vector3 up = (Vector3)up_f3;

            // Apply the position and rotation to the enemy
            transform.position = position;

            // NEW: Apply alignment based on configuration
            if (alignmentMode != AlignmentMode.None)
            {
                Quaternion rotation = CalculateRotation(tangent, up, position);
                transform.rotation = rotation;
            }
            // If AlignmentMode is None, rotation is not changed.
        }
    }

    // NEW: Function to calculate the final rotation based on alignment settings
    Quaternion CalculateRotation(Vector3 splineForward, Vector3 splineUp, Vector3 position)
    {
        // 1. Determine the target world-space forward and up vectors based on AlignmentMode
        Vector3 forward = Vector3.forward;
        Vector3 up = Vector3.up;

        switch (alignmentMode)
        {
            case AlignmentMode.SplineElement:
                // Use the evaluated tangent and up vector from the spline
                forward = splineForward;
                up = splineUp;
                break;

            case AlignmentMode.SplineObject:
                // Align to the spline container's transform (requires currentRoute to be the container)
                var objectRotation = currentRoute.transform.rotation;
                forward = objectRotation * Vector3.forward;
                up = objectRotation * Vector3.up;
                break;

                // case AlignmentMode.World: // Not used here, but would use World Space vectors
                //    forward = Vector3.forward;
                //    up = Vector3.up;
                //    break;
        }

        // 2. Calculate the rotation required to align the object's forward/up axes to the target forward/up vectors.

        // Get the target local forward/up axes
        Vector3 remappedForward = GetAxis(objectForwardAxis);
        Vector3 remappedUp = GetAxis(objectUpAxis);

        // The Quaternion.LookRotation creates a rotation where Z is the forward vector and Y is the up vector.
        // If the object's local forward/up axes are *not* Z/Y, we need to apply an inverse offset rotation.
        Quaternion axisRemapRotation = Quaternion.Inverse(Quaternion.LookRotation(remappedForward, remappedUp));

        // Create the rotation that aligns world Z to target 'forward' and world Y to target 'up'
        Quaternion targetRotation = Quaternion.LookRotation(forward, up);

        // Combine the target rotation with the axis remapping
        return targetRotation * axisRemapRotation;
    }

    // NEW: Helper to convert AlignAxis enum to Vector3 (based on SplineAnimate internal logic)
    Vector3 GetAxis(AlignAxis axis)
    {
        switch (axis)
        {
            case AlignAxis.XAxis: return Vector3.right;
            case AlignAxis.YAxis: return Vector3.up;
            case AlignAxis.ZAxis: return Vector3.forward;
            case AlignAxis.NegativeXAxis: return Vector3.left;
            case AlignAxis.NegativeYAxis: return Vector3.down;
            case AlignAxis.NegativeZAxis: return Vector3.back;
        }
        return Vector3.forward; // Default
    }

    // NEW: Helper to ensure forward and up axes are not the same (based on SplineAnimate internal logic)
    void EnsureDifferentAxes(ref AlignAxis forwardAxis, ref AlignAxis upAxis)
    {
        // Check if the axes are the same or opposite (e.g., X and -X)
        if (forwardAxis == upAxis || (int)forwardAxis % 3 == (int)upAxis % 3)
        {
            // If they are the same, default up to Y (if forward is not Y), or Z (if forward is Y)
            if (forwardAxis == AlignAxis.YAxis)
            {
                upAxis = AlignAxis.ZAxis;
            }
            else
            {
                upAxis = AlignAxis.YAxis;
            }
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