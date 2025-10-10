using UnityEngine;
using System.Collections; // Needed for coroutines, though not used here, often included.

/// <summary>
/// A homing missile that automatically seeks the player.
/// It has two phases: active guidance (tracking) and post-guidance (flies straight until destruction).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyHomingMissile : MonoBehaviour
{
    // Properties are typically set by the EnemyMSL script upon instantiation
    [HideInInspector] public int damage;
    [HideInInspector] public GameObject owner;

    [Header("Missile Homing Settings")]
    [Tooltip("The speed at which the missile travels.")]
    public float speed = 15f;
    [Tooltip("The speed at which the missile rotates to track the target.")]
    public float homingRotationSpeed = 5f;

    [Tooltip("The duration (in seconds) that the missile actively tracks the player.")]
    public float guidanceTime = 3f;

    [Tooltip("The time (in seconds) the missile continues flying straight after guidance expires before self-destructing.")]
    public float timeAfterGuidance = 1.0f;

    private Rigidbody rb;
    private Transform target;

    private float totalLifetime;
    private float timeElapsed = 0f;
    private bool isGuided = true; // Tracks whether homing rotation should be applied

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component not found on the missile.", this);
            enabled = false;
            return;
        }

        // Ensure the Rigidbody is set up
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Calculate the total time the missile will exist
        totalLifetime = guidanceTime + timeAfterGuidance;

        // Find the player once at the start
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            target = playerObject.transform;
        }
        else
        {
            Debug.LogWarning("Player not found (tag 'Player'). Missile will fly straight.");
        }
    }

    void Start()
    {
        // Removed initial velocity set here. Velocity is now set in FixedUpdate.
    }

    void Update()
    {
        // Increment time elapsed
        timeElapsed += Time.deltaTime;

        // 1. Check for Guidance Expiration
        if (isGuided && timeElapsed >= guidanceTime)
        {
            isGuided = false;

            // --- CRITICAL FIX: FREEZE ROTATION ---
            // Explicitly set the angular velocity to zero to stop any wild spinning 
            // the moment guidance is disabled, ensuring it glides straight.
            if (rb != null)
            {
                rb.angularVelocity = Vector3.zero;
            }
            // ------------------------------------

            Debug.Log($"Missile Guidance Expired. Rotation maintained. Flying straight for {timeAfterGuidance} seconds.");
        }

        // 2. Check for Final Destruction
        if (timeElapsed >= totalLifetime)
        {
            Destroy(gameObject);
        }
    }

    void FixedUpdate()
    {
        // Only apply homing rotation if a target exists AND the missile is still in its guidance phase
        if (target != null && isGuided)
        {
            ApplyHoming();
        }

        // CRITICAL FIX: Always update the velocity based on the current forward direction.
        // This guarantees continuous, full-speed movement whether homing is active or disabled.
        rb.velocity = transform.forward * speed;
    }

    private void ApplyHoming()
    {
        // 1. Get the target position
        Vector3 targetPosition = target.position;

        // 2. CRITICAL FIX: FORCE TARGET Y TO MISSILE'S Y
        targetPosition.y = transform.position.y;

        // Calculate the direction needed to point towards the target
        Vector3 directionToTarget = (targetPosition - transform.position).normalized;

        // Calculate the rotation needed to point towards the target
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

        // Smoothly rotate towards the target direction
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * homingRotationSpeed);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Get the EnemyProps component (assuming it exists)
        AircraftController player = collision.gameObject.GetComponent<AircraftController>();

        // Prevent hitting the owner or other enemies
        if (collision.gameObject == owner || collision.gameObject.CompareTag("Enemy"))
        {
            return;
        }

        // Check if the missile hit the player or terrain/boundary
        if (collision.gameObject.CompareTag("Player"))
        {
            // Apply damage logic here (if hitting player)
            // Destroy the missile after impact
            Destroy(gameObject);
        }
    }
}
