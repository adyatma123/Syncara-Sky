using UnityEngine;
using System; // Required for events if you add any health-related events later

/// <summary>
/// Controls the movement, weapons, and health of an aircraft.
/// </summary>
public class AircraftController : MonoBehaviour
{
    [Header("Aircraft Health")]
    [Tooltip("The maximum health for this aircraft.")]
    public int maxHealth = 1000;
    [Tooltip("The current health of this aircraft. Automatically set to maxHealth at start.")]
    public int currentHealth; // Renamed from 'health' for clarity

    [Header("Movement Settings")]
    public float movSpeed = 10f;
    public float rotSpeed = 5f;
    public float maxRotAngle = 45f;

    [Header("Weapon Systems")]
    public MissileController missileController;
    public RocketController rocketController;

    private Vector3 targetPosition;
    private bool hasTargetPosition = false;

    /// <summary>
    /// Initializes the aircraft's current health and other starting properties.
    /// </summary>
    void Start()
    {
        currentHealth = maxHealth; // Initialize current health to max health
        Debug.Log($"Aircraft '{gameObject.name}' health initialized: {currentHealth}/{maxHealth}");
    }

    /// <summary>
    /// Called once per frame. Handles movement, rotation, and other continuous logic.
    /// </summary>
    void Update()
    {
        if (hasTargetPosition)
        {
            MoveTowardsTarget();
            RotateTowardsMovement();
        }
        else
        {
            RotateBackToDefault();
        }
    }

    /// <summary>
    /// Sets a new target position for the aircraft to move towards.
    /// </summary>
    /// <param name="target">The world position the aircraft should move to.</param>
    public void SetTargetPosition(Vector3 target)
    {
        targetPosition = target;
        hasTargetPosition = true;
    }

    /// <summary>
    /// Moves the aircraft towards its current target position.
    /// </summary>
    void MoveTowardsTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * movSpeed);
    }

    /// <summary>
    /// Rotates the aircraft to face its current movement direction.
    /// </summary>
    void RotateTowardsMovement()
    {
        Vector3 directionToTarget = targetPosition - transform.position;
        Vector3 projectedDirection = Vector3.ProjectOnPlane(directionToTarget, Vector3.up).normalized;

        if (projectedDirection != Vector3.zero)
        {
            float targetAngleZ = -Mathf.Atan2(projectedDirection.x, projectedDirection.z) * Mathf.Rad2Deg;
            float clampedAngleZ = Mathf.Clamp(targetAngleZ, -maxRotAngle, maxRotAngle);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, clampedAngleZ), Time.deltaTime * rotSpeed);
        }
    }

    /// <summary>
    /// Reduces the aircraft's current health by the specified amount.
    /// Destroys the aircraft if health drops to zero or below.
    /// </summary>
    /// <param name="damageAmount">The amount of damage to inflict.</param>
    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(currentHealth, 0); // Ensure health doesn't go below 0
        Debug.Log($"Aircraft '{gameObject.name}' took {damageAmount} damage. Current Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Increases the aircraft's current health by the specified amount, up to maxHealth.
    /// </summary>
    /// <param name="healAmount">The amount of health to restore.</param>
    public void Heal(int healAmount)
    {
        currentHealth += healAmount;
        currentHealth = Mathf.Min(currentHealth, maxHealth); // Ensure health doesn't exceed max health
        Debug.Log($"Aircraft '{gameObject.name}' healed {healAmount} health. Current Health: {currentHealth}/{maxHealth}");
    }


    /// <summary>
    /// Resets the aircraft's rotation to its default (identity) orientation.
    /// </summary>
    public void ResetRotation()
    {
        hasTargetPosition = false;
        RotateBackToDefault();
    }

    /// <summary>
    /// Smoothly rotates the aircraft back to its default (forward) orientation.
    /// </summary>
    void RotateBackToDefault()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, Time.deltaTime * rotSpeed);
    }

    /// <summary>
    /// Triggers the missile launch sequence, if a MissileController is assigned.
    /// </summary>
    public void FireMissile()
    {
        if (missileController != null)
        {
            missileController.LaunchMissile();
        }
        else
        {
            Debug.LogWarning("MissileController not assigned to AircraftController!");
        }
    }

    /// <summary>
    /// Triggers the rocket launch sequence, if a RocketController is assigned.
    /// </summary>
    public void FireRocket()
    {
        if (rocketController != null)
        {
            rocketController.LaunchRocket();
        }
        else
        {
            Debug.LogWarning("RocketController not assigned to AircraftController!");
        }
    }

    /// <summary>
    /// Handles the aircraft's destruction sequence.
    /// </summary>
    private void Die()
    {
        Debug.Log($"Aircraft '{gameObject.name}' has been destroyed!");
        Destroy(gameObject); // Destroy the aircraft GameObject
    //Example: Play explosion sound effect
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
