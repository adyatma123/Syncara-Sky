using System;
using UnityEngine;

// Add this interface definition outside the main class. 
// Any specific enemy movement script (e.g., ForwardMoveBehavior.cs) MUST implement this interface.
public interface IEnemyBehavior { }

/// <summary>
/// This MonoBehaviour component acts as the runtime data holder for an enemy instance.
/// It retrieves its base properties from an assigned 'EnemyData' ScriptableObject
/// and manages the enemy's current health and damage-taking logic.
/// </summary>
public class EnemyProps : MonoBehaviour
{
    // --- STATIC EVENTS FOR GAME MANAGER AND STORY EVENT MANAGER ---

    /// <summary>Fires when ANY enemy is destroyed (by player or out-of-bounds).</summary>
    public static event Action OnEnemyDestroyed;

    /// <summary>Fires when enemy is destroyed specifically by a player projectile (passes score value).</summary>
    public static event Action<int> OnEnemyDestroyedByPlayerScore;

    // ----------------------------------------------------------------------------------

    [Header("Data Source")]
    [Tooltip("Assign the EnemyData ScriptableObject asset that defines this enemy type's properties.")]
    public EnemyData enemyDataSource; // Reference to the EnemyData ScriptableObject

    // Private fields to store the properties synchronized from EnemyData.
    private string _enemyName;
    private int _maxHealth;
    private float _movSpeed;
    private int _enemyDmg;
    private float _fireRate;
    private float _bulletSpeed;
    private int _scoreVal;

    // --- UPDATED FIELD ---
    private EnemyType _enemyType; // Now synchronized from the EnemyData enum
                                  // ---------------------

    private bool _isBoss;
    private bool _isArmedMG;
    private bool _isArmedRKT;
    private bool _isArmedMSL;

    // --- NEW REFERENCES FOR DEATH HAND-OFF ---
    private EnemyController _controller;
    private Rigidbody _rb;
    // CRITICAL FIX: Changed to IEnemyBehavior to find the specific movement script
    private MonoBehaviour _mainBehaviorScript;
    // -----------------------------------------

    [Header("Dynamic State")]
    [Tooltip("The current health of this enemy instance, which changes during gameplay.")]
    public int currentHealth; // This remains public as it's the runtime mutable health

    // Public properties to allow other scripts (like EnemyController) to access the synchronized data.
    public string EnemyName => _enemyName;
    public int MaxHealth => _maxHealth;
    public float MovSpeed => _movSpeed;
    public int EnemyDmg => _enemyDmg;
    public float FireRate => _fireRate;
    public float BulletSpeed => _bulletSpeed;
    public int ScoreVal => _scoreVal;

    // --- UPDATED PROPERTY ---
    public EnemyType EnemyType => _enemyType;
    // ------------------------

    public bool IsBoss => _isBoss;
    public bool IsArmedMG => _isArmedMG;
    public bool IsArmedRKT => _isArmedRKT;
    public bool IsArmedMSL => _isArmedMSL;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// This is used to synchronize data from the ScriptableObject early.
    /// </summary>
    void Awake()
    {
        // Critical: Ensure an EnemyData asset is assigned.
        if (enemyDataSource == null)
        {
            enabled = false; // Disable this component to prevent further errors
            return;
        }

        // Synchronize all properties from the assigned EnemyData ScriptableObject to private fields.
        _enemyName = enemyDataSource.enemyName;
        _maxHealth = enemyDataSource.maxHealth;
        _movSpeed = enemyDataSource.movSpeed;
        _enemyDmg = enemyDataSource.enemyDmg;
        _fireRate = enemyDataSource.fireRate;
        _bulletSpeed = enemyDataSource.bulletSpeed;
        _scoreVal = enemyDataSource.scoreVal;

        // --- SYNCHRONIZE NEW ENUM PROPERTY ---
        _enemyType = enemyDataSource.enemyType;
        // -------------------------------------

        _isBoss = enemyDataSource.isBoss;
        _isArmedMG = enemyDataSource.isArmedMG;
        _isArmedRKT = enemyDataSource.isArmedRKT;
        _isArmedMSL = enemyDataSource.isArmedMSL;

        // Initialize the current health to the maximum health defined in EnemyData.
        currentHealth = _maxHealth;

        // Get references to controller and rigidbody for death handling
        _controller = GetComponent<EnemyController>();
        _rb = GetComponent<Rigidbody>();

        // CRITICAL FIX: Find the one script that implements IEnemyBehavior (your specific movement script)
        _mainBehaviorScript = GetComponent<IEnemyBehavior>() as MonoBehaviour;
    }

    /// <summary>
    /// Reduces the enemy's current health by the given amount.
    /// If health drops to zero or below, the enemy is destroyed and reports the kill statistics.
    /// </summary>
    /// <param name="damageAmount">The amount of damage to apply to the enemy's health.</param>
    /// <param name="damageSource">The GameObject that inflicted the damage (e.g., a player's projectile). Can be null.</param>
    public void TakeDamage(int damageAmount, GameObject damageSource = null)
    {
        currentHealth -= damageAmount;

        if (currentHealth <= 0)
        {
            // Ensure this script is active before proceeding (prevents double-death/score)
            if (!enabled) return;

            // ... (Existing effect and score logic) ...

            // 2. Report Total Destruction
            OnEnemyDestroyed?.Invoke();
            OnEnemyDestroyedByPlayerScore.Invoke(_scoreVal);

            // 3. Cleanup: Hand off control to the new death animation system

            // A. Stop existing EnemyController (which cleans up weapons and movement)
            if (_controller != null) _controller.CleanupForDeath();

            // B. Calculate Death Velocity in WORLD SPACE first:
            Vector3 initialWorldVelocity = _rb != null ? _rb.velocity : Vector3.zero;

            // We want to preserve horizontal (X) and forward (Z) momentum, but enforce the fall (Y)
            // and the backward push (Z) based on MovSpeed.

            // 1. Define the desired momentum (World Space)
            Vector3 worldSpaceMomentum = new Vector3(
                initialWorldVelocity.x,     // Keep current World X momentum
                initialWorldVelocity.y,     // Keep current World Y momentum (this could be zero if not falling)
                initialWorldVelocity.z      // Keep current World Z momentum
            );

            // 2. Define the enforced LOCAL Death Impulse. 
            // We want the plane to drop down (-Y) and drift backward (-Z) relative to its nose.
            Vector3 enforcedLocalImpulse = new Vector3(
                0f,                         // No enforced local X movement
                -_movSpeed,                 // Enforce local DOWNWARD fall at MovSpeed
                -_movSpeed                  // Enforce local BACKWARD drift at MovSpeed
            );

            // 3. Convert the Local Impulse into World Space.
            Vector3 enforcedWorldImpulse = transform.TransformDirection(enforcedLocalImpulse);

            // 4. Combine the initial world momentum with the world impulse (this is the final World Vector)
            Vector3 combinedWorldVelocity = worldSpaceMomentum + enforcedWorldImpulse;

            // 5. CRITICAL STEP: Convert the FINAL combined World Velocity back to LOCAL SPACE.
            // The AfterDeathAnimation script can then apply this Local Vector to the local transform.
            Vector3 finalLocalDeathVelocity = transform.InverseTransformDirection(combinedWorldVelocity);


            // C. Add the AfterDeathAnimation component and initialize
            AfterDeathAnimation deathAnimation = gameObject.AddComponent<AfterDeathAnimation>();
            // PASS THE FINAL LOCAL DEATH VELOCITY
            deathAnimation.Initialize(EnemyType, finalLocalDeathVelocity, _mainBehaviorScript);

            // D. Disable this EnemyProps script to prevent further damage or events
            enabled = false;

            // ... (rest of the code) ...
        }
    }

    public static void ReportEnemyDestroyed(int score)
    {
        OnEnemyDestroyed?.Invoke();
        OnEnemyDestroyedByPlayerScore?.Invoke(score);
    }
}
