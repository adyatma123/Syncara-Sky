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

        // --- MG SETUP ---
        if (enemyProps.IsArmedMG)
        {
            // Initializing fire time for machine gun
            nextFireTimeMG = Time.time + (60f / enemyProps.FireRate);
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
        if (enemyProps.IsArmedMG && Time.time >= nextFireTimeMG)
        {
            if (useBurstFire)
            {
                StartCoroutine(FireBurst(bulletPrefab, enemyProps.BulletSpeed));
            }
            else
            {
                ShootProjectile(bulletPrefab, enemyProps.BulletSpeed);
            }

            // Calculate the time for the NEXT single shot/burst start
            nextFireTimeMG = Time.time + (60f / enemyProps.FireRate);
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
        Debug.Log($"[{gameObject.name}] Projectile launched at {Time.time}!");
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
            }
            else
            {
                Debug.LogWarning($"Instantiated prefab {projectilePrefab.name} is missing a required script (EnemyHomingMissile or EnemyBullet).");
            }
        }

        // Example of playing an SFX:
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("Shoot");
        }
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
                nextFireTimeMSL = Time.time + delayBetweenMissiles;
            }

            // Wait for the fire rate duration before checking again
            yield return new WaitForSeconds(delayBetweenMissiles);
        }
        Debug.Log($"Missile routine: Loop stopped (isShooting is false).");
    }
}
