using UnityEngine;
using System.Collections;

/// <summary>
/// Handles the Machine Gun (MG) firing logic for the enemy.
/// This runs independently in the Update loop, supporting single and burst fire modes.
/// </summary>
public class EnemyMG : MonoBehaviour
{
    [Tooltip("The bullet prefab to instantiate.")]
    public GameObject bulletPrefab;

    [Header("Burst Fire Settings")]
    [Tooltip("Check to enable burst fire instead of a single shot.")]
    public bool useBurstFire = false;

    [Tooltip("The number of shots fired in a single burst.")]
    [Range(2, 5)] // Enforces a minimum of 2 and maximum of 5 in the Inspector slider
    public int burstShotCount = 3;

    [Tooltip("The time (in seconds) between each shot in a burst.")]
    public float timeBetweenBurstShots = 0.1f;

    [Header("Weapon Setup")]
    [Tooltip("The Transform from where projectiles will be instantiated.")]
    public Transform firePoint;

    private EnemyProps enemyProps;
    private float nextFireTimeMG;
    private bool isShooting = false;
    private Coroutine currentBurstCoroutine; // Reference for stopping burst mid-fire

    void Awake()
    {
        enemyProps = GetComponent<EnemyProps>();
    }

    public void Activate()
    {


        if (enemyProps == null || !enemyProps.IsArmedMG) return;
        if (isShooting) return;

        isShooting = true;

        // Set the next fire time to the current time to fire immediately on the first Update() check.
        nextFireTimeMG = Time.time;
    }

    public void Deactivate()
    {
        if (!isShooting) return;

        isShooting = false;

        if (currentBurstCoroutine != null)
        {
            StopCoroutine(currentBurstCoroutine);
            currentBurstCoroutine = null;
        }
    }

    void Update()
    {
        // DEBUG CHECK 1: Is Update running?
        if (Time.frameCount % 60 == 0)
        {
            // Log every 60 frames to prevent spam, confirming Update() execution
            //Debug.Log($"[{gameObject.name}] MG Update running. isShooting: {isShooting}.");
        }

        // DEBUG CHECK 2: Should we exit early?
        if (!isShooting || enemyProps == null || !enemyProps.IsArmedMG) return;



        // Calculate the required delay based on FireRate (RPM)
        float fireInterval = (60f / enemyProps.FireRate);

        // DEBUG CHECK 4: Is it time to fire?
        if (Time.time >= nextFireTimeMG)
        {
            // DEBUG CHECK 5: Final trigger confirmation

            if (useBurstFire)
            {
                // Ensure only one burst coroutine runs at a time
                if (currentBurstCoroutine == null)
                {
                    currentBurstCoroutine = StartCoroutine(FireBurst(bulletPrefab, enemyProps.BulletSpeed));
                }
            }
            else
            {
                // Fire a single shot
                ShootBullet(bulletPrefab, enemyProps.BulletSpeed);
            }

            // CRITICAL: Ensure next fire time increments by the fixed interval
            nextFireTimeMG = Time.time + fireInterval;
        }
    }

    private void ShootBullet(GameObject projectilePrefab, float projectileSpeed)
    {

        // Instantiate the projectile at the firePoint's position and rotation
        GameObject instantiatedProjectile = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);

        // Assume it's a standard bullet
        EnemyBullet bulletScript = instantiatedProjectile.GetComponent<EnemyBullet>();
        if (bulletScript != null && enemyProps != null)
        {
            bulletScript.damage = (int)enemyProps.EnemyDmg;
            bulletScript.owner = this.gameObject;
            Vector3 shootDirection = firePoint.forward;
            bulletScript.SetDirectionAndSpeed(shootDirection, projectileSpeed);
        }

        // Example of playing an SFX:
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("Player Shoot");
        }
    }

    private IEnumerator FireBurst(GameObject projectilePrefab, float projectileSpeed)
    {
        for (int i = 0; i < burstShotCount; i++)
        {
            ShootBullet(projectilePrefab, projectileSpeed);
            yield return new WaitForSeconds(timeBetweenBurstShots);
        }
        // Burst finished, clear the coroutine reference
        currentBurstCoroutine = null;
    }
}
