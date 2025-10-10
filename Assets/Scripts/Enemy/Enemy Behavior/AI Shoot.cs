using UnityEngine;
using System.Collections;

public class AIShoot : MonoBehaviour
{
    [Tooltip("The bullet prefab to instantiate (used for IsArmedMG).")]
    public GameObject bulletPrefab;

    [Tooltip("The missile prefab to instantiate (used for IsArmedMSL).")]
    public GameObject missilePrefab;

    [Header("Burst Fire Settings (MG)")]
    [Tooltip("Check to enable burst fire instead of a single shot.")]
    public bool useBurstFire = false;

    [Tooltip("The number of shots fired in a single burst.")]
    [Range(2, 5)] // Enforces a minimum of 2 and maximum of 5 in the Inspector slider
    public int burstShotCount = 3;

    [Tooltip("The time (in seconds) between each shot in a burst.")]
    public float timeBetweenBurstShots = 0.1f;

    [Header("Weapon Setup")]
    [Tooltip("The Transform from where projectiles will be instantiated. Can be a child GameObject.")]
    public Transform firePoint;

    [Header("Missile Timing (MSL)")]
    [Tooltip("Initial delay before the first missile launches after activation (in seconds).")]
    public float initialMissileLaunchDelay = 1.0f;

    private EnemyProps enemyProps;
    private float nextFireTimeMG;
    private float nextFireTimeMSL;
    private bool isShooting = false;
    private Coroutine missileLaunchCoroutine;

    void Awake()
    {
        enemyProps = GetComponent<EnemyProps>();
        if (enemyProps == null)
        {
            Debug.LogError("AIShoot requires an EnemyProps component on the parent GameObject.", this);
            enabled = false;
        }

        if (firePoint == null)
        {
            Debug.LogError("AIShoot requires a Fire Point Transform assigned in the Inspector.", this);
            enabled = false;
        }

        isShooting = false;
    }

    // OnBecameVisible/Invisible methods are intentionally empty, as state is managed by EnemyController.cs

    public void Activate()
    {
        if (isShooting) return;

        isShooting = true;
        Debug.Log($"[{gameObject.name}] ACTIVATED shooting.");

        // --- MG SETUP: Initialize fire time here, the Update loop will handle the rest. ---
        if (enemyProps.IsArmedMG)
        {
            // Set the next fire time to the current time, allowing it to fire immediately 
            // on the first Update() call if ready.
            nextFireTimeMG = Time.time;
            Debug.Log($"MG initialized. Next fire time set to: {nextFireTimeMG}");
        }

        // --- MISSILE SETUP ---
        if (enemyProps.IsArmedMSL && missilePrefab != null)
        {
            if (missileLaunchCoroutine != null) StopCoroutine(missileLaunchCoroutine);
            missileLaunchCoroutine = StartCoroutine(LaunchMissileRoutine());
        }
    }

    public void Deactivate()
    {
        if (!isShooting) return;

        isShooting = false;
        Debug.Log($"[{gameObject.name}] DEACTIVATED shooting. Stopping all fire.");

        // Stop all ongoing coroutines (like bursts or missile launches)
        StopAllCoroutines();
        missileLaunchCoroutine = null;
    }

    void Update()
    {
        if (!isShooting) return;

        // --- MACHINE GUN LOGIC ---
        if (enemyProps.IsArmedMG)
        {
            if (bulletPrefab == null)
            {
                // This check is important, ensures we don't proceed without a projectile.
                Debug.LogWarning($"MG ARMED but bulletPrefab is NULL on {gameObject.name}. Cannot fire MG.");
                return;
            }

            // Calculate the required delay based on FireRate (RPM)
            float fireInterval = (60f / enemyProps.FireRate);

            // Fire only if current time is past the next calculated fire time
            if (Time.time >= nextFireTimeMG)
            {
                if (useBurstFire)
                {
                    // Start the burst coroutine
                    StartCoroutine(FireBurst(bulletPrefab, enemyProps.BulletSpeed));
                }
                else
                {
                    // Fire a single shot
                    ShootProjectile(bulletPrefab, enemyProps.BulletSpeed);
                }

                // *** CRITICAL FIX: Ensure next fire time increments by the fixed interval ***
                // This reliably calculates the next time the weapon should be ready.
                nextFireTimeMG = Time.time + fireInterval;
                // *** END CRITICAL FIX ***

                // Add debug logs inside the condition to reduce console spam
                if (useBurstFire)
                {
                    Debug.Log($"MG Burst fired. Next burst start time: {nextFireTimeMG}");
                }
                else
                {
                    Debug.Log($"MG Single fired. Next shot time: {nextFireTimeMG}");
                }
            }
        }
    }

    // Renamed and updated to handle the generic projectile shooting logic
    private void ShootProjectile(GameObject projectilePrefab, float projectileSpeed)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"Cannot shoot: Projectile prefab is null. Enemy: {gameObject.name}");
            return;
        }

        // --- DEBUG LOG: Confirming Actual Shot ---
        // This log now confirms a generic projectile shot, MG or MSL.
        Debug.Log($"[{gameObject.name}] Projectile '{projectilePrefab.name}' launched at {Time.time}!");
        // ------------------------------------------

        // Instantiate the projectile at the firePoint's position and rotation
        GameObject instantiatedProjectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        // Try to get the appropriate script (EnemyHomingMissile or EnemyBullet)
        EnemyHomingMissile missileScript = instantiatedProjectile.GetComponent<EnemyHomingMissile>();

        if (missileScript != null)
        {
            // If it's a homing missile, initialize its properties
            missileScript.damage = (int)enemyProps.EnemyDmg;
            missileScript.owner = this.gameObject;

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX("Player Missile");
            }
        }
        else
        {
            // Assume it's a standard bullet
            EnemyBullet bulletScript = instantiatedProjectile.GetComponent<EnemyBullet>();
            if (bulletScript != null && enemyProps != null)
            {
                bulletScript.damage = (int)enemyProps.EnemyDmg;
                bulletScript.owner = this.gameObject;
                Vector3 shootDirection = firePoint.forward;
                bulletScript.SetDirectionAndSpeed(shootDirection, projectileSpeed);

                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySFX("Player Shoot");
                }
            }
            else
            {
                Debug.LogWarning($"Instantiated prefab {projectilePrefab.name} is missing a required script (EnemyHomingMissile or EnemyBullet).");
            }
        }

        // Example of playing an SFX:
        
    }

    private IEnumerator FireBurst(GameObject projectilePrefab, float projectileSpeed)
    {
        for (int i = 0; i < burstShotCount; i++)
        {
            ShootProjectile(projectilePrefab, projectileSpeed);
            yield return new WaitForSeconds(timeBetweenBurstShots);
        }
    }

    private IEnumerator LaunchMissileRoutine()
    {
        // --- DEBUG LOG: Confirms Missile Coroutine START ---
        Debug.Log($"[{gameObject.name}] Missile Launch Coroutine STARTED.");

        // 1. Initial delay before the first missile is launched
        Debug.Log($"Missile armed. Initial delay of {initialMissileLaunchDelay} seconds before first launch...");
        yield return new WaitForSeconds(initialMissileLaunchDelay);
        // --- DEBUG LOG: Confirms Initial Delay COMPLETE ---
        Debug.Log($"Missile routine: Initial delay complete at {Time.time}. Starting continuous loop.");

        // Convert fire rate (BPM) to delay (seconds)
        float delayBetweenMissiles = (60f / enemyProps.FireRate);

        // 2. Continuous firing loop
        while (isShooting)
        {
            // --- DEBUG LOG: Confirms Checkpoint Before Launch ---
            Debug.Log($"Missile routine: Checking payload. IsArmedMSL: {enemyProps.IsArmedMSL}, MissilePrefab: {missilePrefab != null}.");

            if (enemyProps.IsArmedMSL && missilePrefab != null)
            {
                ShootProjectile(missilePrefab, 0f); // Speed is ignored for homing missile as its script handles it
                // nextFireTimeMSL is calculated by the yield at the end of the loop, no need to set here
            }

            // Wait for the fire rate duration before checking again
            yield return new WaitForSeconds(delayBetweenMissiles);
        }
        Debug.Log($"Missile routine: Loop stopped (isShooting is false).");
    }
}
