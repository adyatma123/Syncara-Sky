using UnityEngine;
using System; // Required for Action event

/// <summary>
/// ScriptableObject to define the base properties for different enemy types.
/// This allows you to create multiple enemy data assets with unique stats
/// without creating new prefabs for each stat variation.
/// </summary>
[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Enemy/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Enemy Core Properties")]
    public string enemyName = "Default Enemy"; // Default name for the enemy type
    [Tooltip("The maximum health for this enemy type.")]
    public int maxHealth = 100;
    [Tooltip("The movement speed for this enemy type.")]
    public float movSpeed = 5f; // Changed to float for more precise movement
    [Tooltip("The damage dealt by this enemy's attacks/bullets.")]
    public int enemyDmg = 1;
    [Tooltip("The fire rate of this enemy's attacks (e.g., bullets per minute).")]
    public float fireRate = 60f; // Default 60 RPM (1 shot per second)
    [Tooltip("The speed at which the bullet travels.")]
    public float bulletSpeed = 200f;
    [Tooltip("The score value awarded to the player when this enemy is destroyed.")]
    public int scoreVal = 100;

    [Header("Enemy Type Specifics")]
    [Tooltip("Set to true if this enemy is a helicopter type (or flies).")]
    public bool isHelicopter = false;
    public bool isBoss = false;
    public bool isArmedMG = false;
    public bool isArmedRKT = false;
    public bool isArmedMSL = false;
}