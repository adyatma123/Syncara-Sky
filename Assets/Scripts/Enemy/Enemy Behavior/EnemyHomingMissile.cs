using UnityEngine;

/// <summary>
/// Controls a homing missile projectile shot by an enemy.
/// The missile tracks the player for a limited duration and maintains a constant speed.
/// 
/// REQUIRES: A Rigidbody component for physics-based movement.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyHomingMissile : MonoBehaviour
{
    [Header("Homing Settings")]
    [Tooltip("The speed at which the missile travels.")]
    public float speed = 15f;
    [Tooltip("The rotational speed for homing (how quickly it turns to track the player).")]
    public float homingRotationSpeed = 5f;
    [Tooltip("The time (in seconds) the missile will actively track the target before self-destructing/flying straight.")]
    public float guidanceTime = 5f;

    // Runtime state
    private Rigidbody rb;
    private Transform playerTarget;
    private float currentGuidanceTimer;

    // Properties typically set by the spawning enemy
    [HideInInspector] public int damage = 1;
    [HideInInspector] public GameObject owner;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component not found. HomingMissile requires a Rigidbody.", this);
            enabled = false;
        }

        // Initialize timer
        currentGuidanceTimer = guidanceTime;

        // Ensure Rigidbody settings are correct
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }

    void Start()
    {
        // 1. Find the player (assumes the player has the tag "Player")
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTarget = player.transform;
        }
        else
        {
            Debug.LogWarning("Player not found! Missile will fly straight.");
        }

        // 2. Give the missile its initial forward velocity
        if (rb != null)
        {
            rb.velocity = transform.forward * speed;
        }
    }

    void Update()
    {
        // Decrement the guidance timer
        currentGuidanceTimer -= Time.deltaTime;

        // Check if guidance time has expired
        if (currentGuidanceTimer <= 0f && playerTarget != null)
        {
            // Stop tracking after the guidance time runs out
            playerTarget = null;
            Debug.Log($"{gameObject.name}: Guidance time expired. Flying straight.");
        }

        // Basic self-destruction outside the guidance system (e.g., if it goes way off-screen)
        // You can use your EnemyController's boundary Z check logic here if needed, 
        // but for a bullet/missile, a simple distance or timer check is often enough.
        if (currentGuidanceTimer < -5f) // Destroy 5 seconds after guidance ends
        {
            Destroy(gameObject);
        }
    }

    void FixedUpdate()
    {
        // Only home if we have a target
        if (playerTarget != null)
        {
            // Calculate the direction vector to the target
            Vector3 directionToTarget = (playerTarget.position - transform.position).normalized;

            // Calculate the rotation needed to look at the target
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

            // Smoothly rotate the missile's current rotation towards the target rotation
            rb.rotation = Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                Time.fixedDeltaTime * homingRotationSpeed
            );

            // Apply the forward velocity based on the new rotation
            rb.velocity = transform.forward * speed;
        }
        else
        {
            // If guidance is lost, maintain current forward momentum/velocity
            rb.velocity = transform.forward * speed;
        }
    }

    // Example: Collision logic
    void OnTriggerEnter(Collider other)
    {
        // Prevent hitting the owner enemy or other enemies
        if (other.gameObject == owner || other.CompareTag("Enemy"))
        {
            return;
        }

        // Check if it hit the player
        if (other.CompareTag("Player"))
        {
            // Add damage logic here (e.g., calling player health script)
            Debug.Log($"Missile hit Player! Applying {damage} damage.");

            // Destroy the missile on impact
            Destroy(gameObject);
        }
    }
}
