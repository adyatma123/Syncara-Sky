using UnityEngine;
using System.Linq;

/// <summary>
/// A manually guided missile that moves forward and follows player horizontal movement.
/// This script receives its properties from a Payload ScriptableObject at runtime.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ManualGuidedMissile : MonoBehaviour
{
    private Rigidbody rb;
    private float currentGuidanceTime;

    [Tooltip("The Payload ScriptableObject that defines this missile's properties.")]
    public Payload payloadData;

    [Header("Visuals")]
    [Tooltip("The child GameObject that will perform the rotation.")]
    public Transform missileModel;
    [Tooltip("The maximum rotation angle around the Y-axis.")]
    public float maxRotationAngle = 30f;
    [Tooltip("The speed at which the missile rotates, for smooth transitions.")]
    public float rotationSmoothSpeed = 5f;

    private string missileName;
    private int mDamage;
    private float speed;
    private float lifeTime;
    private float rotationSpeed;
    private Transform playerTransform; // Reference to the player's transform

    private float lastPositionX;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component not found on the missile. GuidedMissile script requires a Rigidbody to function.", this);
            enabled = false;
            return;
        }

        if (payloadData == null)
        {
            Debug.LogError("No Payload ScriptableObject assigned to the GuidedMissile component. This missile will not function correctly.", this);
            enabled = false;
            return;
        }

        if (missileModel == null)
        {
            Debug.LogError("Missile Model Transform is not assigned. Please assign the child GameObject for banking.", this);
            enabled = false;
            return;
        }

        // Initialize all properties from the assigned Payload ScriptableObject
        missileName = payloadData.payloadName;
        mDamage = payloadData.damage;
        speed = payloadData.speed;
        lifeTime = payloadData.lifeTime;
        rotationSpeed = payloadData.rotationSpeed;
        currentGuidanceTime = lifeTime;

        rb.isKinematic = false;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Find the player's transform. Assumes the player has the tag "Player".
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("Player GameObject with tag 'Player' not found. Guided missile will not follow.");
        }

        // Disable unneeded axis rotation and Y movement
        rb.constraints = RigidbodyConstraints.FreezeRotationX
               | RigidbodyConstraints.FreezeRotationY
               | RigidbodyConstraints.FreezeRotationZ
               | RigidbodyConstraints.FreezePositionY;
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
    }

    void FixedUpdate()
    {
        // Follow the player's horizontal position
        if (playerTransform != null)
        {
            Vector3 targetPosition = new Vector3(playerTransform.position.x, transform.position.y, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, rotationSpeed * Time.fixedDeltaTime);
        }

        // The missile always moves forward
        rb.velocity = transform.forward * speed;

        // Calculate the current X velocity for Y-rotation
        float currentX = transform.position.x;
        float xVelocity = (currentX - lastPositionX) / Time.fixedDeltaTime;
        lastPositionX = currentX;

        // Determine the target rotation angle based on horizontal velocity
        float targetYRotation = 0f;
        if (xVelocity > 0.01f)
        {
            targetYRotation = maxRotationAngle;
        }
        else if (xVelocity < -0.01f)
        {
            targetYRotation = -maxRotationAngle;
        }

        // Smoothly rotate the child model towards the target rotation on the Y-axis
        Quaternion currentRotation = missileModel.localRotation;
        float newYRotation = Mathf.LerpAngle(currentRotation.eulerAngles.y, targetYRotation, rotationSmoothSpeed * Time.fixedDeltaTime);
        missileModel.localRotation = Quaternion.Euler(currentRotation.eulerAngles.x, newYRotation, currentRotation.eulerAngles.z);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Only destroy the missile and apply damage if it hits an enemy.
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log($"{missileName} hit " + collision.gameObject.name);
            EnemyProps enemy = collision.gameObject.GetComponent<EnemyProps>();
            if (enemy != null)
            {
                enemy.TakeDamage(mDamage);
            }

            if (VisualEffectManager.Instance != null && !string.IsNullOrEmpty("Payload Impact"))
            {
                // Spawn the effect at the enemy's position and current rotation
                VisualEffectManager.Instance.PlayEffect("Payload Impact", transform.position, transform.rotation);
            }

            Destroy(gameObject);
        }
    }
}
