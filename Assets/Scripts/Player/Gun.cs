using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

public class Gun : MonoBehaviour
{
    // The currently active Scriptable Object containing all gun stats (Damage, RoF, Bullet Prefab, Heat Increase per shot)
    public Guns guns;

    // --- Gun component references (REVISED) ---
    [Header("Spawn Points")]
    [Tooltip("Spawn points for Gun Stage 1")]
    public Transform[] bulletSpawnPoint1;
    [Tooltip("Spawn points for Gun Stage 2")]
    public Transform[] bulletSpawnPoint2;
    [Tooltip("Spawn points for Gun Stage 3")]
    public Transform[] bulletSpawnPoint3;

    // REMOVED: ParticleSystem[] muzzleFlashes1, 2, 3 arrays are removed since the VFX Manager handles them.

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
    [Tooltip("Minimum heat level required for cooldown to end and allow firing again (Specific to the vehicle's cooling system).")]
    public float overheatMinCD = 30f;

    // Internal state variables
    public float currentHeat = 0f;
    private bool gunOverheated = false;

    // UI Visuals
    public float blinkDuration = 1f;
    private float blinkTimer = 0f;

    // Spread is still local, but could be moved to the Guns SO later
    public float gunSpread = 0.1f;
    public int gunStage = 1;
    public int totalGunActive;

    // Default cooldown rate if not firing (can be adjusted in the Inspector)
    [Header("Heat Cooldown Rate (Constant)")]
    public float passiveCooldownRate = 20f;

    void Awake()
    {
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

            // Assuming your 'Gun' script has a method to apply properties:
            ApplyGunProperties(confirmedGunData);

            Debug.Log($"Gun Component Initialized: Applied confirmed gun '{confirmedGunData.name}' from Selection Manager.");
        }

        if (VhcChgr.selectedGunData != null)
        {
            ApplyGunProperties(VhcChgr.selectedGunData);
            // Clear the static reference if it's not needed after loading, 
            // to prevent issues if you return to the menu and select a new vehicle.
            VhcChgr.selectedGunData = null;
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

        // Reset heat state. Note: maxHeat and overheatMinCD are NOT reset here as they are local.
        currentHeat = 0f;
        gunOverheated = false;
        if (overheatText != null) overheatText.enabled = false;
        Debug.Log($"Gun successfully switched to: {guns.name}");
    }

    void Update()
    {
        if (guns == null) return; // Prevent errors if no gun is assigned

        UpdateActiveGunCount();

        // --- Weapon Stage Cycle Logic (Remains unchanged) ---
        if (Input.GetKeyDown(KeyCode.C))
        {
            gunStage++;
            if (gunStage > 4)
            {
                gunStage = 1;
            }
            Debug.Log("Current Stage: " + gunStage);
        }

        // --- Firing Logic (Uses SO heatRate, but local maxHeat) ---
        if (!gunOverheated)
        {
            if (Input.GetButton("Gun") && Time.time >= nextFireTime)
            {
                Shoot();
                // Use RateOfFire from the SO
                nextFireTime = Time.time + (60f / guns.rateOfFire);
                // Assuming SoundManager.Instance exists
                // Note: SFX is called here once per fire interval, which is good.
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

        // --- Cooldown Logic (Uses local passiveCooldownRate) ---
        if (currentHeat > 0)
        {
            // Use local passiveCooldownRate for heat reduction
            currentHeat -= Time.deltaTime * passiveCooldownRate;
            currentHeat = Mathf.Max(0, currentHeat);
        }

        // Check for cooldown completion (using local overheatMinCD)
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
            UpdateActiveGunCount();

            // --- STAGE 1: Iterate through all active spawn points and fire/play VFX ---
            if (gunStage == 1)
            {
                foreach (Transform spawnPoint in bulletSpawnPoint1)
                {
                    if (spawnPoint.gameObject.activeInHierarchy)
                    {
                        FireBullet(spawnPoint);
                        PlayShotVFX(spawnPoint);
                    }
                }
                // REMOVED: foreach (ParticleSystem muzzleFlash in muzzleFlashes1) muzzleFlash.Play();
            }
            // --- STAGE 2: Iterate through all active spawn points and fire/play VFX ---
            else if (gunStage == 2)
            {
                foreach (Transform spawnPoint in bulletSpawnPoint2)
                {
                    if (spawnPoint.gameObject.activeInHierarchy)
                    {
                        FireBullet(spawnPoint);
                        PlayShotVFX(spawnPoint);
                    }
                }
                // REMOVED: foreach (ParticleSystem muzzleFlash in muzzleFlashes2) muzzleFlash.Play();
            }
            // --- STAGE 3: Use SP1 and SP2 ---
            else if (gunStage == 3)
            {
                foreach (Transform spawnPoint in bulletSpawnPoint1) { if (spawnPoint.gameObject.activeInHierarchy) { FireBullet(spawnPoint); PlayShotVFX(spawnPoint); } }
                // REMOVED: foreach (ParticleSystem muzzleFlash in muzzleFlashes1) muzzleFlash.Play();
                foreach (Transform spawnPoint in bulletSpawnPoint2) { if (spawnPoint.gameObject.activeInHierarchy) { FireBullet(spawnPoint); PlayShotVFX(spawnPoint); } }
                // REMOVED: foreach (ParticleSystem muzzleFlash in muzzleFlashes2) muzzleFlash.Play();
            }
            // --- STAGE 4: Use SP1, SP2, and SP3 ---
            else if (gunStage == 4)
            {
                foreach (Transform spawnPoint in bulletSpawnPoint1) { if (spawnPoint.gameObject.activeInHierarchy) { FireBullet(spawnPoint); PlayShotVFX(spawnPoint); } }
                // REMOVED: foreach (ParticleSystem muzzleFlash in muzzleFlashes1) muzzleFlash.Play();
                foreach (Transform spawnPoint in bulletSpawnPoint2) { if (spawnPoint.gameObject.activeInHierarchy) { FireBullet(spawnPoint); PlayShotVFX(spawnPoint); } }
                // REMOVED: foreach (ParticleSystem muzzleFlash in muzzleFlashes2) muzzleFlash.Play();
                foreach (Transform spawnPoint in bulletSpawnPoint3) { if (spawnPoint.gameObject.activeInHierarchy) { FireBullet(spawnPoint); PlayShotVFX(spawnPoint); } }
                // REMOVED: foreach (ParticleSystem muzzleFlash in muzzleFlashes3) muzzleFlash.Play();
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

    private void UpdateActiveGunCount()
    {
        totalGunActive = 0; // Reset count

        // Count active spawn points across all stages
        if (gunStage == 1) { foreach (Transform sp in bulletSpawnPoint1) if (sp.gameObject.activeInHierarchy) totalGunActive++; }
        if (gunStage == 2) { foreach (Transform sp in bulletSpawnPoint2) if (sp.gameObject.activeInHierarchy) totalGunActive++; }
        if (gunStage == 3)
        {
            foreach (Transform sp in bulletSpawnPoint1) if (sp.gameObject.activeInHierarchy) totalGunActive++;
            foreach (Transform sp in bulletSpawnPoint2) if (sp.gameObject.activeInHierarchy) totalGunActive++;
        }
        if (gunStage == 4)
        {
            foreach (Transform sp in bulletSpawnPoint1) if (sp.gameObject.activeInHierarchy) totalGunActive++;
            foreach (Transform sp in bulletSpawnPoint2) if (sp.gameObject.activeInHierarchy) totalGunActive++;
            foreach (Transform sp in bulletSpawnPoint3) if (sp.gameObject.activeInHierarchy) totalGunActive++;
        }
    }
}
