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

    [SerializeField]
    private float interceptRefreshInterval = 0.2f; // seconds

    private string missileName;
    private int mDamage;
    private float speed;
    private float homingRotationSpeed;
    private float lockRadius;
    private float maxHomingAngle;
    private float guidanceTime;
    private Vector3 cachedInterceptPoint;
    private float interceptRefreshTimer;
    

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
    }

    void Update()
    {
        // Decrement the guidance timer
        currentGuidanceTime -= Time.deltaTime;

        // Destroy the missile when the guidance time runs out
        if (currentGuidanceTime <= 0)
        {
            Destroy(gameObject);
        }

        // Find the nearest enemy if we don't have a target or have lost our lock
        if (target == null)
        {
            FindNearestEnemy();
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
        float nearestScore = Mathf.Infinity; // Use a 'score' instead of just distance

        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                float score = distance;

                // Penalty: If the enemy is already targeted, penalize its 'score' by multiplying
                // its distance by a factor (e.g., 2.0x). This makes the missile prefer
                // an untargeted enemy even if it's slightly farther away.
                if (collider.GetComponent<TargetedByMissile>() != null)
                {
                    score *= 2.0f; // Adjust this multiplier as needed
                }

                if (score < nearestScore)
                {
                    nearestScore = score;
                    nearest = collider.transform;
                }
            }
        }

        if (nearest != null)
        {
            // Set the target and activate homing
            target = nearest;
            isHoming = true;

            // RESERVE THE TARGET: Add the marker component
            if (target.GetComponent<TargetedByMissile>() == null)
            {
                target.gameObject.AddComponent<TargetedByMissile>();
            }
        }
    }

    private Vector3 GetTargetVelocity()
    {
        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        return targetRb != null ? targetRb.velocity : Vector3.zero;
    }

    private void ApplyHoming()
    {
        float distanceToTarget =
            Vector3.Distance(transform.position, target.position);

        Vector3 toTarget =
            (target.position - transform.position).normalized;

        float angleToTarget =
            Vector3.Angle(transform.forward, toTarget);

        if (distanceToTarget > lockRadius || angleToTarget > maxHomingAngle)
        {
            var marker = target.GetComponent<TargetedByMissile>();
            if (marker != null) Destroy(marker);

            isHoming = false;
            target = null;
            return;
        }

        Vector3 targetVelocity = GetTargetVelocity();
        Vector3 rawToTarget = target.position - transform.position;

        float distance = rawToTarget.magnitude;
        float timeToIntercept = distance / speed;

        Vector3 predictedPosition = target.position + targetVelocity * timeToIntercept;

        Vector3 desiredDirection = (predictedPosition - transform.position).normalized;

        float angleToPredicted =
            Vector3.Angle(transform.forward, desiredDirection);

        if (angleToPredicted > maxHomingAngle)
        {
            desiredDirection = transform.forward;
        }

        Quaternion targetRotation = Quaternion.LookRotation(desiredDirection);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.fixedDeltaTime * homingRotationSpeed
        );

        rb.velocity = transform.forward * speed;
    }


    void OnCollisionEnter(Collision collision)
    {
        BossWeakpoint wp = collision.gameObject.GetComponent<BossWeakpoint>();

        if (wp != null)
        {
            wp.TakeDamage(mDamage);

            Destroy(gameObject);
            return;
        }

        // --- FIX: IGNORE COLLISION WITH PLAYER TAG ---
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log($"{missileName}: Ignoring collision with Player.");
            return; // Exit without applying damage or destroying the missile
        }
        // ---------------------------------------------


        // Only destroy the missile if it hits an enemy.
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log($"{missileName} hit " + collision.gameObject.name);
            EnemyProps enemy = collision.gameObject.GetComponent<EnemyProps>();
            if (enemy != null)
            {
                enemy.TakeDamage(mDamage, this.gameObject); // Assuming TakeDamage needs the source
            }

            // RELEASE THE TARGET: Remove the component when the target is destroyed or hit
            var marker = collision.gameObject.GetComponent<TargetedByMissile>();
            if (marker != null)
            {
                Destroy(marker);
            }

            if (VisualEffectManager.Instance != null && !string.IsNullOrEmpty("Payload Impact"))
            {
                // Spawn the effect at the enemy's position and current rotation
                VisualEffectManager.Instance.PlayEffect("Payload Impact", transform.position, transform.rotation);
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
