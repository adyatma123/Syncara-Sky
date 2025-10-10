using System;
using UnityEngine;

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

    // Event triggered when this enemy is destroyed by the player, passing the score value.

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
            // 1. Check for Player Kill and Report Score
            // We use the "PlayerProjectile" tag (as established previously) for accurate scoring.
            if (damageSource != null && damageSource.CompareTag("PlayerProjectile"))
            {
                OnEnemyDestroyedByPlayerScore?.Invoke(_scoreVal); // Fires event for Game Manager to add score and increment player kills
            }

            // 2. Report Total Destruction (for total count, wave clearing, and StoryEventManager)
            OnEnemyDestroyed?.Invoke();

            // 3. Cleanup
            Destroy(gameObject);

            // Example of playing an SFX:
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX("Explode");
            }
        }
    }
}
