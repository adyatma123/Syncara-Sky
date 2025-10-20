using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

/// <summary>
/// Handles the randomized death animation logic for enemies,
/// including immediate destruction, simple fall, and rotational fall (for helicopters).
/// This component is added dynamically when an enemy's health reaches zero.
/// Destruction is now triggered by colliding with an object tagged "Ground" rather than a fixed Y position.
/// NOTE: The EnemyType enum is assumed to be defined externally (e.g., in EnemySO.cs).
/// </summary>
public class AfterDeathAnimation : MonoBehaviour
{
    [Header("Animation Toggles")]
    [Tooltip("If true, the enemy can be destroyed immediately without animation.")]
    public bool enableDestroyImmediately = true;
    [Tooltip("If true, the enemy can fall straight down with initial velocity.")]
    public bool enableFallStraight = false;
    [Tooltip("If true, the enemy can fall while rotating (only for Helicopter).")]
    public bool enableFallAndRotate = false;


    [Header("Fall Configuration")]
    [Tooltip("The speed of rotational tumbling for helicopters (degrees per second).")]
    public float tumbleSpeed = 180f;

    // Runtime variables
    private EnemyType _enemyType;
    private Vector3 _initialLocalVelocity;
    private MonoBehaviour _mainBehaviorScript;
    private bool _isFalling = false;
    private bool _isTumbling = false;

    // NEW: Public property that Aimbot will check. 
    // It's set to true immediately upon initialization since this component only exists if the enemy is dead.
    public bool IsDead { get; private set; } = false;

    // Random choice variables (Constants remain for internal logic clarity)
    private const int DESTROY_IMMEDIATELY = 0;
    private const int FALL_STRAIGHT = 1;
    private const int FALL_AND_ROTATE = 2;
    private int _deathAnimationChoice;


    /// <summary>
    /// Initializes the death animation state.
    /// </summary>
    /// <param name="type">The type of enemy, used to determine if tumbling is allowed.</param>
    /// <param name="localVelocity">The calculated local space velocity to begin the movement.</param>
    /// <param name="activeBehaviorScript">The main movement script to disable.</param>
    public void Initialize(EnemyType type, Vector3 localVelocity, MonoBehaviour activeBehaviorScript)
    {
        // NEW: Set the definitive dead flag immediately.
        IsDead = true;

        _enemyType = type;
        _initialLocalVelocity = localVelocity;

        // 1. Disable the active behavior script if it exists
        if (activeBehaviorScript != null)
        {
            activeBehaviorScript.enabled = false;
        }

        // --- Logic to select an animation from enabled options ---

        List<int> availableChoices = new List<int>();

        if (enableDestroyImmediately)
        {
            availableChoices.Add(DESTROY_IMMEDIATELY);
        }
        if (enableFallStraight)
        {
            availableChoices.Add(FALL_STRAIGHT);
        }
        if (enableFallAndRotate)
        {
            availableChoices.Add(FALL_AND_ROTATE);
        }

        // 1. Force immediate destruction if NO options are enabled
        if (availableChoices.Count == 0)
        {
            _deathAnimationChoice = DESTROY_IMMEDIATELY;
        }
        // 2. Select the ONLY available option (FORCE IT)
        else if (availableChoices.Count == 1) // <-- NEW: Pengecekan eksplisit
        {
            _deathAnimationChoice = availableChoices[0];
        }
        // 3. Randomly select if MORE THAN ONE option is enabled
        else // availableChoices.Count > 1
        {
            int randomIndex = Random.Range(0, availableChoices.Count);
            _deathAnimationChoice = availableChoices[randomIndex];
        }

        // ---------------------------------------------------------------------

        switch (_deathAnimationChoice)
        {
            case DESTROY_IMMEDIATELY:
                // Behavior 1: Just destroyed (no animation).
                // Use a minimal coroutine (0.05s) to allow external destruction effects to initialize.
                StartCoroutine(DestroyAfterDelay(0.05f));
                break;

            case FALL_STRAIGHT:
                // Behavior 2: Keep moving towards current velocity but falling down.
                _isFalling = true;
                break;

            case FALL_AND_ROTATE:
                // Behavior 3: Falling while rotating (only for Helicopter).
                if (_enemyType.ToString() == "Helicopter")
                {
                    _isFalling = true;
                    _isTumbling = true;
                }
                else
                {
                    // Fallback to simpler fall if tumbling was chosen but the enemy is not a Helicopter.
                    _deathAnimationChoice = FALL_STRAIGHT;
                    _isFalling = true;
                }
                break;
        }

        // Disable rendering/colliders and setup for trigger check
        SetupVisualsAndPhysicsForFall();
    }

    /// <summary>
    /// Disables original colliders, visuals, and rigid body physics.
    /// Re-enables the primary collider as a trigger for ground detection.
    /// </summary>
    private void SetupVisualsAndPhysicsForFall()
    {
        // 1. Disable all colliders initially
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        // 2. Re-enable the main/first collider as a trigger
        Collider mainCollider = GetComponent<Collider>();
        if (mainCollider != null)
        {
            mainCollider.enabled = true;
            mainCollider.isTrigger = true; // Use trigger for smooth destruction without physics conflict
        }

        // 3. Ensure Rigidbody is kinematic or disabled to prevent conflicting physics.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }


    void Update()
    {
        if (!_isFalling) return;

        // 1. Movement: Apply the initial local velocity in the local space of the object.
        transform.Translate(_initialLocalVelocity * Time.deltaTime, Space.Self);

        // 2. Rotation (Tumbling)
        if (_isTumbling)
        {
            // Apply a constant rotation around the object's local X and Z axes (tumbling effect)
            transform.Rotate(tumbleSpeed * Time.deltaTime, 0, tumbleSpeed * Time.deltaTime, Space.Self);
        }

        // *** Catatan: Pengecekan ground Y boundary dihilangkan. Deteksi akan dilakukan di OnTriggerEnter. ***
    }

    /// <summary>
    /// Called when the primary collider (set as trigger) hits another collider.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Hanya hancurkan jika sedang jatuh DAN menabrak objek yang ditandai sebagai "Ground".
        if (_isFalling && other.CompareTag("Ground"))
        {
            // Hentikan jatuhan
            _isFalling = false;

            // Lakukan efek penghancuran
            DoDestructionEffects();

            // Hancurkan objek ini
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Executes the destruction visuals and sound effects.
    /// </summary>
    private void DoDestructionEffects()
    {
        if (VisualEffectManager.Instance != null && !string.IsNullOrEmpty("Aircraft Explode"))
        {
            // Spawn the effect at the enemy's position and current rotation
            VisualEffectManager.Instance.PlayEffect("Aircraft Explode", transform.position, transform.rotation);
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("Explode");
        }
    }


    /// <summary>
    /// Coroutine to wait for a short, fixed delay before destroying the object.
    /// Used only for the DESTROY_IMMEDIATELY case (0.05s).
    /// </summary>
    private System.Collections.IEnumerator DestroyAfterDelay(float delay)
    {
        DoDestructionEffects();
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}