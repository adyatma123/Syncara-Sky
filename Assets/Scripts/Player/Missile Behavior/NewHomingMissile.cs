using UnityEngine;
using System.Linq;

/// <summary>
/// A homing missile that automatically seeks the nearest enemy within range.
/// This script receives its properties from a Payload ScriptableObject at runtime.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class HomingMissile : MonoBehaviour
{
    private bool isHoming = false;
    private Rigidbody rb;
    private Transform target;
    private float currentGuidanceTime;

    [Tooltip("The Payload ScriptableObject that defines this missile's properties.")]
    public Payload payloadData;

    private string missileName;
    private int mDamage;
    private float speed;
    private float homingRotationSpeed;
    private float lockRadius;
    private float maxHomingAngle;
    private float guidanceTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component not found on the missile. HomingMissile script requires a Rigidbody to function.", this);
            enabled = false;
            return;
        }

        if (payloadData == null)
        {
            Debug.LogError("No Payload ScriptableObject assigned to the HomingMissile component. This missile will not function correctly.", this);
            enabled = false;
            return;
        }

        // Initialize all properties from the assigned Payload ScriptableObject
        missileName = payloadData.payloadName;
        mDamage = payloadData.damage;
        speed = payloadData.speed;
        homingRotationSpeed = payloadData.rotationSpeed;
        lockRadius = payloadData.lockRadius;
        maxHomingAngle = payloadData.maxHomingAngle;
        guidanceTime = payloadData.lifeTime;
        currentGuidanceTime = guidanceTime;

        // Ensure the Rigidbody is non-kinematic and uses continuous collision detection
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void Start()
    {
        // Give the missile an initial forward push
        rb.velocity = transform.forward * speed;

        Debug.Log($"Missile '{missileName}' launched. Seeking target...");
    }

    void Update()
    {
        // Decrement the guidance timer
        currentGuidanceTime -= Time.deltaTime;

        // Destroy the missile when the guidance time runs out
        if (currentGuidanceTime <= 0)
        {
            Debug.Log($"{missileName}: Guidance time expired. Self-destructing.");
            Destroy(gameObject);
        }

        // Find the nearest enemy if we don't have a target or have lost our lock
        if (target == null)
        {
            FindNearestEnemy();
            if (target != null)
            {
                Debug.Log($"{missileName}: Found target '{target.name}'. Homing activated.");
            }
        }
    }

    void FixedUpdate()
    {
        // Only apply homing if a target exists and is within range and angle
        if (isHoming && target != null)
        {
            ApplyHoming();
        }
    }

    private void FindNearestEnemy()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, lockRadius);
        Transform nearest = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Collider collider in colliders)
        {
            // Use CompareTag for performance and safety
            if (collider.CompareTag("Enemy"))
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = collider.transform;
                }
            }
        }

        if (nearest != null)
        {
            // Set the target and activate homing
            target = nearest;
            isHoming = true;
        }
    }

    private void ApplyHoming()
    {
        // Check for loss of homing lock
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

        if (distanceToTarget > lockRadius || angleToTarget > maxHomingAngle)
        {
            isHoming = false;
            target = null;
            Debug.Log($"{missileName}: Homing lock lost. Target is out of range or angle.");
            return;
        }

        // Calculate the rotation needed to point towards the target
        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

        // Smoothly rotate towards the target direction
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * homingRotationSpeed);

        // Update the missile's velocity to maintain constant speed
        rb.velocity = transform.forward * speed;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if the missile hit a Player to prevent unintended destruction
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log($"{missileName} hit player, but will not be destroyed.");
            return;
        }

        // Only destroy the missile if it hits an enemy.
        // Otherwise, it passes through.
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log($"{missileName} hit " + collision.gameObject.name);
            EnemyProps enemy = collision.gameObject.GetComponent<EnemyProps>();
            if (enemy != null)
            {
                enemy.TakeDamage(mDamage);
            }
            // Destroy the missile after hitting the enemy
            Destroy(gameObject);
        }
    }

    // --- GIZMOS FOR VISUAL DEBUGGING ---
    void OnDrawGizmosSelected()
    {
        if (payloadData == null) return;

        // Gizmo for the lock radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, payloadData.lockRadius);

        // Draw the field-of-view cone
        Gizmos.color = Color.yellow;
        Vector3 forward = transform.forward;
        float coneAngleRad = payloadData.maxHomingAngle * Mathf.Deg2Rad;

        int segments = 12;
        Vector3 lastPoint = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * 360f;
            Quaternion rotation = Quaternion.AngleAxis(angle, forward);
            Vector3 direction = rotation * (Quaternion.Euler(payloadData.maxHomingAngle, 0, 0) * Vector3.forward);

            Vector3 currentPoint = transform.position + direction.normalized * payloadData.lockRadius;
            if (i > 0)
            {
                Gizmos.DrawLine(lastPoint, currentPoint);
            }
            lastPoint = currentPoint;

            Gizmos.DrawLine(transform.position, currentPoint);
        }
    }
}
