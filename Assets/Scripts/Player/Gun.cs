using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

// 1. NEW SERIALIZABLE CLASS FOR GUN LAYOUT
[System.Serializable]
public class GunStageLayout
{
    [Tooltip("The name of this firing stage (e.g., 'Single', 'Dual', 'Full Salvo').")]
    public string stageName = "Stage";
    [Tooltip("The transforms that fire when this stage is active. This array must contain the cumulative transforms for stages where that is intended.")]
    public Transform[] spawnPoints;
}
// ------------------------------------------

public class Gun : MonoBehaviour
{
    // The currently active Scriptable Object containing all gun stats (Damage, RoF, Bullet Prefab, Heat Increase per shot)
    public Guns guns;

    // --- Gun component references (REVISED) ---
    [Header("Gun Layouts")]
    [Tooltip("Defines the arrangement of spawn points for each gun stage (Stage 1 is index 0, Stage 2 is index 1, etc.).")]
    // 2. REPLACED old Transform[] arrays with a flexible List of stages
    public List<GunStageLayout> gunStages = new List<GunStageLayout>();

    // REMOVED: Old bulletSpawnPoint1, 2, 3 arrays.

    [Header("Visual Effects")]
    [Tooltip("The unique name of the VFX to play for the gun shot (e.g., 'TracerEffect'). This replaces the muzzle flash arrays.")]
    public string shotVFXName = "Muzzle Flash";
    // --- END REVISED REFERENCES ---

    public TextMeshProUGUI overheatText;
    public Aimbot aimbot;

    private float nextFireTime;

    // --- LOCAL HEAT MANAGEMENT (Aircraft Specific) ---
    [Header("Aircraft Heat Capacity")]
    [Tooltip("Maximum heat before the gun disables (Specific to the vehicle's cooling system).")]
    public float maxHeat = 100f;

    [Header("Cooldown Threshold (20% of Max Heat)")]
    private float overheatMinCD;

    // Internal state variables
    public float currentHeat = 0f;
    private bool gunOverheated = false;

    // UI Visuals
    public float blinkDuration = 1f;
    private float blinkTimer = 0f;

    // Spread is still local, but could be moved to the Guns SO later
    public float gunSpread = 0.1f;
    // gunStage is now a 1-based index (1 to Count) into the gunStages list
    public int gunStage = 1;
    public int totalGunActive;

    // Default cooldown rate if not firing (can be adjusted in the Inspector)
    [Header("Heat Cooldown Rate (Constant)")]
    public float passiveCooldownRate = 20f; // This variable is now unused in the new calculation, but kept for inspector visibility.

    void Awake()
    {
        // Calculate the minimum cooldown threshold
        overheatMinCD = maxHeat * 0.20f;

        // Automatically find the TextMeshProUGUI with the "OverheatText" tag
        GameObject overheatTextObject = GameObject.FindWithTag("OverheatText");
        if (overheatTextObject != null)
        {
            overheatText = overheatTextObject.GetComponent<TextMeshProUGUI>();
            if (overheatText != null)
            {
                overheatText.enabled = false;
            }
            else
            {
                Debug.LogError("The GameObject with tag 'OverheatText' does not have a TextMeshProUGUI component.");
            }
        }

        if (GameSelectionManager.Instance != null && GameSelectionManager.Instance.ConfirmedGunSelection != null)
        {
            Guns confirmedGunData = GameSelectionManager.Instance.ConfirmedGunSelection;
            ApplyGunProperties(confirmedGunData);
            Debug.Log($"Gun Component Initialized: Applied confirmed gun '{confirmedGunData.name}' from Selection Manager.");
        }

        if (VhcChgr.selectedGunData != null)
        {
            ApplyGunProperties(VhcChgr.selectedGunData);
            VhcChgr.selectedGunData = null;
        }

        // NEW: Initial Aimbot speed update if an Aimbot is linked
        if (aimbot != null && guns != null)
        {
            aimbot.CurrentBulletSpeed = guns.bulletSpeed;
        }

        // Ensure the initial stage is valid
        if (gunStages.Count > 0)
        {
            gunStage = Mathf.Clamp(gunStage, 1, gunStages.Count);
        }
        else
        {
            gunStage = 0; // Invalid stage if no layouts are defined
        }
    }

    /// <summary>
    /// PUBLIC: Called by the GunSelector to change the weapon's properties dynamically.
    /// This function switches the active Guns Scriptable Object.
    /// </summary>
    /// <param name="newGuns">The Guns Scriptable Object to be selected and applied.</param>
    public void ApplyGunProperties(Guns newGuns)
    {
        if (newGuns == null)
        {
            Debug.LogError("Cannot apply null Gun data!");
            return;
        }

        // --- Core Modularity Step: Assign the new data container ---
        this.guns = newGuns;

        // Recalculate based on vehicle's max heat
        overheatMinCD = maxHeat * 0.20f;

        currentHeat = 0f;
        gunOverheated = false;
        if (overheatText != null) overheatText.enabled = false;

        if (aimbot != null)
        {
            aimbot.CurrentBulletSpeed = guns.bulletSpeed;
        }
    }

    void Update()
    {
        if (guns == null) return; // Prevent errors if no gun is assigned

        UpdateActiveGunCount();

        // --- Weapon Stage Cycle Logic (Updated to use the new List count) ---
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (gunStages.Count > 0)
            {
                gunStage++;
                // If it exceeds the number of defined stages, loop back to Stage 1
                if (gunStage > gunStages.Count)
                {
                    gunStage = 1;
                }
                Debug.Log($"Current Stage: Stage {gunStage} - {gunStages[gunStage - 1].stageName}");
            }
            else
            {
                Debug.LogWarning("No gun stages are defined in the 'Gun Layouts' list!");
                gunStage = 0; // Indicate no active stage
            }
        }

        // --- Firing Logic (Uses SO heatRate, but local maxHeat) ---
        // Only allow firing if a stage is active (gunStage > 0)
        if (!gunOverheated && gunStage > 0)
        {
            if (Input.GetButton("Gun") && Time.time >= nextFireTime)
            {
                Shoot();
                // Use RateOfFire from the SO
                nextFireTime = Time.time + (60f / guns.rateOfFire);

                SoundManager.Instance.PlaySFX(guns.ShootSoundKey);

                // Increase heat (using heatRate from SO and totalGunActive, compared against local maxHeat)
                currentHeat += guns.heatRate * totalGunActive;

                if (currentHeat >= this.maxHeat) // Use local maxHeat
                {
                    gunOverheated = true;
                    if (overheatText != null) overheatText.enabled = true;
                    blinkTimer = 0f;
                    Debug.Log($"{guns.name} Overheated on this aircraft!");
                }
            }
        }

        // --- Overheat Blink Logic (Remains unchanged) ---
        if (gunOverheated && overheatText != null)
        {
            blinkTimer += Time.deltaTime;

            float alpha;
            if (blinkTimer <= blinkDuration / 2f) alpha = Mathf.Sin((blinkTimer / (blinkDuration / 2f)) * Mathf.PI / 2f);
            else alpha = Mathf.Cos(((blinkTimer - blinkDuration / 2f) / (blinkDuration / 2f)) * Mathf.PI / 2f);

            Color textColor = overheatText.color;
            textColor.a = alpha;
            overheatText.color = textColor;

            if (blinkTimer >= blinkDuration) blinkTimer -= blinkDuration;
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (aimbot != null) aimbot.enabled = !aimbot.enabled;
        }

        // --- Cooldown Logic (REVISED to 1% of maxHeat per second) ---
        if (currentHeat > 0)
        {
            // Calculate 20% of maxHeat (the cooldown amount per second)
            float cooldownPerSecond = maxHeat * 0.2f;

            // Reduce heat
            currentHeat -= Time.deltaTime * cooldownPerSecond;
            currentHeat = Mathf.Max(0, currentHeat);
        }

        // Check for cooldown completion (using calculated overheatMinCD)
        if (gunOverheated && currentHeat <= this.overheatMinCD) // Use local overheatMinCD
        {
            gunOverheated = false;
            if (overheatText != null)
            {
                Color textColor = overheatText.color;
                textColor.a = 1f;
                overheatText.color = textColor;
                overheatText.enabled = false;
            }
        }

        void Shoot()
        {
            // 3. SIMPLIFIED SHOOT LOGIC
            UpdateActiveGunCount();

            // Convert 1-based gunStage to 0-based index
            int index = gunStage - 1;

            if (index >= 0 && index < gunStages.Count)
            {
                // Get the spawn points for the current, fully defined stage
                Transform[] currentSpawnPoints = gunStages[index].spawnPoints;

                // Iterate through all active spawn points in the selected layout and fire/play VFX
                foreach (Transform spawnPoint in currentSpawnPoints)
                {
                    // Always check for null since transforms can be deleted in the editor
                    if (spawnPoint != null && spawnPoint.gameObject.activeInHierarchy)
                    {
                        FireBullet(spawnPoint);
                        PlayShotVFX(spawnPoint);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Cannot fire. Invalid stage index: {gunStage}. Check 'Gun Layouts' configuration.");
            }
        }
    }

    /// <summary>
    /// Helper method to call the VisualEffectManager for the gun shot effect.
    /// </summary>
    private void PlayShotVFX(Transform spawnPoint)
    {
        if (VisualEffectManager.Instance != null && !string.IsNullOrEmpty(shotVFXName))
        {
            // Call the visual effect at the spawn point's position and rotation
            VisualEffectManager.Instance.PlayEffect(shotVFXName, spawnPoint.position, spawnPoint.rotation);
        }
    }

    // Helper function to fire a bullet (Uses SO properties)
    private void FireBullet(Transform spawnPoint)
    {
        // Calculate random X rotation offset
        float randomXRotation = Random.Range(-gunSpread, gunSpread);

        // Create a new rotation by adding the random offset to the existing X rotation
        Quaternion bulletRotation = spawnPoint.rotation;
        bulletRotation *= Quaternion.Euler(0f, randomXRotation, 0f);

        // Use the bullet prefab from the *active* Guns SO
        GameObject bulletInstance = Instantiate(guns.bulletPrefab, spawnPoint.position, spawnPoint.rotation);

        bulletInstance.tag = "PlayerProjectile";

        // Pass damage from the SO
        Bullet bulletScript = bulletInstance.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.damage = guns.damage;
        }

        // Use the bullet speed from the *active* Guns SO
        Rigidbody rb = bulletInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = bulletRotation * Vector3.forward * guns.bulletSpeed;
        }
    }

    // 4. SIMPLIFIED ACTIVE GUN COUNT LOGIC
    private void UpdateActiveGunCount()
    {
        totalGunActive = 0; // Reset count

        // Convert 1-based gunStage to 0-based index
        int index = gunStage - 1;

        // Check if the current stage index is valid
        if (index >= 0 && index < gunStages.Count)
        {
            // Get the spawn points for the current stage
            Transform[] currentSpawnPoints = gunStages[index].spawnPoints;

            // Count active spawn points
            foreach (Transform sp in currentSpawnPoints)
            {
                // Check if the transform reference is valid and active in the hierarchy
                if (sp != null && sp.gameObject.activeInHierarchy)
                {
                    totalGunActive++;
                }
            }
        }
    }
}
