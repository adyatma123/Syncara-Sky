using System;
using UnityEngine;

/// <summary>
/// This MonoBehaviour component acts as the runtime data holder for an enemy instance.
/// It retrieves its base properties from an assigned 'EnemyData' ScriptableObject
/// and manages the enemy's current health and damage-taking logic.
/// </summary>
public class EnemyProps : MonoBehaviour
{
    [Header("Data Source")]
    [Tooltip("Assign the EnemyData ScriptableObject asset that defines this enemy type's properties.")]
    public EnemyData enemyDataSource; // Reference to the EnemyData ScriptableObject

    // Private fields to store the properties synchronized from EnemyData.
    // They are not directly editable in the Inspector for clarity, as they come from the ScriptableObject.
    private string _enemyName;
    private int _maxHealth;
    private float _movSpeed;
    private int _enemyDmg;
    private float _fireRate;
    private float _bulletSpeed;
    private int _scoreVal;
    private bool _isHelicopter;
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
    public bool IsHelicopter => _isHelicopter;
    public bool IsArmedMG => _isArmedMG;
    public bool IsArmedRKT => _isArmedRKT;
    public bool IsArmedMSL => _isArmedMSL;

    // Event triggered when this enemy is destroyed by the player, passing the score value.
    public event Action<int> OnEnemyDestroyedByPlayer;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// This is used to synchronize data from the ScriptableObject early.
    /// </summary>
    void Awake()
    {
        // Critical: Ensure an EnemyData asset is assigned.
        if (enemyDataSource == null)
        {
            Debug.LogError($"EnemyDataSource is not assigned to EnemyProps on {gameObject.name}! Please assign an EnemyData asset. This enemy will not function correctly.", this);
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
        _isHelicopter = enemyDataSource.isHelicopter;
        _isArmedMG = enemyDataSource.isArmedMG;
        _isArmedRKT = enemyDataSource.isArmedRKT;
        _isArmedMSL = enemyDataSource.isArmedMSL;

        // Initialize the current health to the maximum health defined in EnemyData.
        currentHealth = _maxHealth;

        Debug.Log($"[{_enemyName}] EnemyProps synchronized from '{enemyDataSource.name}'. Initial Health: {currentHealth}/{_maxHealth}. Speed: {_movSpeed}");
    }

    /// <summary>
    /// Reduces the enemy's current health by the given amount.
    /// If health drops to zero or below, the enemy is destroyed, and the OnEnemyDestroyedByPlayer event
    /// is triggered if the damage source was tagged as "Player".
    /// </summary>
    /// <param name="damageAmount">The amount of damage to apply to the enemy's health.</param>
    /// <param name="damageSource">The GameObject that inflicted the damage (e.g., a player's bullet). Can be null.</param>
    public void TakeDamage(int damageAmount, GameObject damageSource = null)
    {
        currentHealth -= damageAmount;
        Debug.Log($"[{_enemyName}] took {damageAmount} damage. Current Health: {currentHealth}");

        if (currentHealth <= 0)
        {
            // Check if the damage source was the player before invoking the score event.
            if (damageSource != null && damageSource.CompareTag("Player"))
            {
                OnEnemyDestroyedByPlayer?.Invoke(_scoreVal); // Use the synchronized score value
                Debug.Log($"[{_enemyName}] destroyed by player! Awarding {_scoreVal} points.");
            }
            else
            {
                Debug.Log($"[{_enemyName}] destroyed by non-player source or no source provided.");
            }

            // Destroy the GameObject this script is attached to.
            // Any visual explosion effects or sound effects should typically be handled
            // by a separate component or triggered just before destruction (e.g., via a particle system).
            Destroy(gameObject);

            //Example of playing an SFX, assuming an AudioManager.Instance exists:
             if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("Explode");
            }
            else
            {
                Debug.LogWarning("AudioManager.Instance not found. Cannot play 'Explode' SFX.");
            }
        }
    }
}