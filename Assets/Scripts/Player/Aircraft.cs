using UnityEngine;
using System; // Required for events if you add any health-related events later

/// <summary>
/// Controls the movement, weapons, and health of an aircraft.
/// </summary>
public class AircraftController : MonoBehaviour
{
    // NEW: Reference to the ScriptableObject holding the base stats
    [Header("Data Source")]
    [Tooltip("The ScriptableObject containing the base stats for this vehicle.")]
    public Vehicles vehicleData; // This will hold the stats loaded at runtime

    // NEW: Public Property to get the vehicle name from the SO
    public string VehicleName => vehicleData != null ? vehicleData.name : gameObject.name;

    [Header("Aircraft Health")]
    [Tooltip("The maximum health for this aircraft. Loaded from SO if assigned.")]
    public int maxHealth = 1000;
    [Tooltip("The current health of this aircraft. Automatically set to maxHealth at start.")]
    public int currentHealth; // Renamed from 'health' for clarity

    [Header("Movement Settings")]
    [Tooltip("Movement speed. Loaded from SO if assigned.")]
    public float movSpeed = 10f;
    [Tooltip("Rotation speed. Loaded from SO if assigned.")]
    public float rotSpeed = 5f;
    [Tooltip("Maximum roll angle. Loaded from SO if assigned.")]
    public float maxRotAngle = 45f;

    [Header("Weapon Systems")]
    [Tooltip("Reference to the single PayloadManager script on this GameObject.")]
    public PayloadManager payloadManager;

    // NEW: Public reference to the Gun component for debug access
    [Header("Debug References")]
    public Gun controlledGun;

    // NEW: Public accessor for Aimbot status for the debug overlay
    public bool IsAimbotActive => controlledGun != null && controlledGun.aimbot != null && controlledGun.aimbot.enabled;

    private PlayerHealthBar playerHealthBar;
    private Vector3 targetPosition;
    private bool hasTargetPosition = false;

    /// <summary>
    /// Initializes the aircraft's current health and other starting properties.
    /// </summary>
    void Start()
    {
        // 1. Check for Vehicle Data and Apply Stats
        if (vehicleData != null)
        {
            // Apply stats from the ScriptableObject
            ApplyVehicleStats(vehicleData);
        }

        // 2. Initialize Health
        currentHealth = maxHealth; // Initialize current health to max health
        Debug.Log($"Aircraft '{gameObject.name}' health initialized: {currentHealth}/{maxHealth} (Source: {(vehicleData != null ? "SO" : "Inspector")})");

        // Try to find the health bar GameObject in the scene by its tag.
        GameObject healthBarObject = GameObject.FindWithTag("HealthBar"); // Make sure your health bar GameObject has this tag
        if (healthBarObject != null)
        {
            playerHealthBar = healthBarObject.GetComponent<PlayerHealthBar>();
            if (playerHealthBar == null)
            {
                Debug.LogError("PlayerHealthBar component not found on the health bar GameObject. Make sure the script is attached to it.");
            }
        }
        else
        {
            Debug.LogError("PlayerHealthBar GameObject not found in the scene. Ensure it exists and has the 'HealthBar' tag.");
        }

        // Validate the payload manager reference
        if (payloadManager == null)
        {
            Debug.LogError("PayloadManager reference not assigned! Please assign it in the Inspector.");
        }

        // NEW: Find the Gun component on this GameObject
        controlledGun = GetComponent<Gun>();
        if (controlledGun == null)
        {
            Debug.LogError("Gun component not found on the controlled Aircraft! Debug overlay will be incomplete.");
        }
    }

    /// <summary>
    /// Applies movement and health properties from the Vehicles ScriptableObject.
    /// This should be called immediately upon instantiation or in Start().
    /// </summary>
    /// <param name="data">The Vehicles ScriptableObject to load data from.</param>
    public void ApplyVehicleStats(Vehicles data)
    {
        if (data == null)
            return;

        // Assign the SO reference
        vehicleData = data;

        // Apply Health stats
        maxHealth = Mathf.Max(
            1,
            data.health
        );

        // Apply Movement stats
        movSpeed = data.movSpeed;
        rotSpeed = data.rotSpeed;
        maxRotAngle = data.maxRot;

        // Set the GameObject's name from the ScriptableObject
        gameObject.name = data.name;

        Debug.Log($"Vehicle stats loaded successfully from SO: {data.name}");
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
            // Calculate the angle based on the direction of movement
            float targetAngleZ = -Mathf.Atan2(projectedDirection.x, projectedDirection.z) * Mathf.Rad2Deg;

            // This rotation logic seems intended for yaw/roll. Assuming it rotates around Z for roll.
            // If the aircraft needs pitch or yaw, this logic needs refinement based on expected behavior.
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
        // Call the health bar's method to trigger the fade effect
        if (playerHealthBar != null)
        {
            playerHealthBar.OnTakeDamage();
        }
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
    /// <param name="healAmount">A mount of health to restore.</param>
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
    /// Calls the FireCurrentPayload method on the PayloadManager.
    /// </summary>
    public void FirePayload()
    {
        if (payloadManager != null)
        {
            // Assumes FireCurrentPayload() is available on PayloadManager
            payloadManager.FireCurrentPayload();
        }
    }

    /// <summary>
    /// Calls the SwitchPayload method on the PayloadManager.
    /// </summary>
    public void SwitchPayload()
    {
        if (payloadManager != null)
        {
            // Assumes SwitchPayload() is available on PayloadManager
            payloadManager.SwitchPayload();
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
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("Explode");
        }
    }
}
