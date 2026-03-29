using UnityEngine;
using System.Collections;
using System.Linq; // Added for convenience, though not strictly required here

/// <summary>
/// Handles the Machine Gun (MG) firing logic for the enemy.
/// This supports single and burst fire modes, with options for using multiple
/// fire points simultaneously or sequentially.
/// </summary>
public class EnemyMG : MonoBehaviour
{
    [Tooltip("The bullet prefab to instantiate.")]
    public GameObject bulletPrefab;

    [Header("Weapon Setup")]
    [Tooltip("The Transforms from where projectiles will be instantiated.")]
    public Transform[] firePoints; // CHANGED to an Array

    [Tooltip("If true, all fire points shoot at the same time. If false, they shoot in sequence.")]
    public bool simultaneousFire = true; // NEW TOGGLE

    [Header("Burst Fire Settings")]
    [Tooltip("Check to enable burst fire instead of a single shot.")]
    public bool useBurstFire = false;

    [Tooltip("The number of shots fired in a single burst.")]
    [Range(2, 5)] // Enforces a minimum of 2 and maximum of 5 in the Inspector slider
    public int burstShotCount = 3;

    [Tooltip("The time (in seconds) between each shot in a burst.")]
    public float timeBetweenBurstShots = 0.1f;

    private EnemyProps enemyProps;
    private float nextFireTimeMG;
    private bool isShooting = false;
    private Coroutine currentBurstCoroutine;
    private int sequentialFirePointIndex = 0; // NEW: To track the next fire point in sequential mode

    void Awake()
    {
        enemyProps = GetComponent<EnemyProps>();
        if (firePoints == null || firePoints.Length == 0)
        {
            Debug.LogError("No Fire Points assigned to the EnemyMG component.", this);
            enabled = false;
        }
    }

    // Update your Activate method in EnemyMG.cs
    public void Activate()
    {
        // RE-FETCH: Ensure we have the synced data from EnemyProps
        if (enemyProps == null) enemyProps = GetComponent<EnemyProps>();

        // Check armed status AFTER syncing
        if (enemyProps == null || !enemyProps.IsArmedMG)
        {
            Debug.LogWarning($"[EnemyMG] {gameObject.name} failed to activate. ArmedMG: {enemyProps?.IsArmedMG}");
            return;
        }

        if (isShooting) return;

        isShooting = true;
        sequentialFirePointIndex = 0;
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
        if (!isShooting || enemyProps == null || !enemyProps.IsArmedMG) return;

        float fireInterval = (60f / enemyProps.FireRate);

        if (Time.time >= nextFireTimeMG)
        {
            if (useBurstFire)
            {
                if (currentBurstCoroutine == null)
                {
                    // Pass the fire point selection to the coroutine
                    currentBurstCoroutine = StartCoroutine(FireBurst(bulletPrefab, enemyProps.BulletSpeed));
                }
            }
            else
            {
                // Handle single shot with multiple fire points
                HandleSingleShot(bulletPrefab, enemyProps.BulletSpeed);
            }

            nextFireTimeMG = Time.time + fireInterval;
        }
    }

    // NEW METHOD: Handles the logic for firing a single shot (or one cycle of fire points)
    private void HandleSingleShot(GameObject projectilePrefab, float projectileSpeed)
    {
        if (simultaneousFire)
        {
            // Simultaneous: Shoot from ALL fire points
            foreach (Transform fp in firePoints)
            {
                ShootBullet(fp, projectilePrefab, projectileSpeed);
            }
        }
        else
        {
            // Sequential: Shoot from the current fire point only
            Transform currentFirePoint = firePoints[sequentialFirePointIndex];
            ShootBullet(currentFirePoint, projectilePrefab, projectileSpeed);

            // Advance the index for the next shot
            sequentialFirePointIndex = (sequentialFirePointIndex + 1) % firePoints.Length;
        }
    }


    // MODIFIED METHOD: Accepts a specific fire point Transform
    private void ShootBullet(Transform currentFirePoint, GameObject projectilePrefab, float projectileSpeed)
    {
        // Instantiate the projectile at the currentFirePoint's position and rotation
        GameObject instantiatedProjectile = Instantiate(projectilePrefab, currentFirePoint.position, currentFirePoint.rotation);

        EnemyBullet bulletScript = instantiatedProjectile.GetComponent<EnemyBullet>();
        if (bulletScript != null && enemyProps != null)
        {
            bulletScript.damage = (int)enemyProps.EnemyDmg;
            bulletScript.owner = this.gameObject;
            Vector3 shootDirection = currentFirePoint.forward;
            bulletScript.SetDirectionAndSpeed(shootDirection, projectileSpeed);
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("Enemy Shoot");
        }
    }

    // MODIFIED COROUTINE: Now uses the HandleSingleShot method
    private IEnumerator FireBurst(GameObject projectilePrefab, float projectileSpeed)
    {
        for (int i = 0; i < burstShotCount; i++)
        {
            // In burst mode, each 'shot' uses the HandleSingleShot logic:
            // - If simultaneousFire is true, ALL fire points shoot.
            // - If simultaneousFire is false, only the next sequential fire point shoots.
            HandleSingleShot(projectilePrefab, projectileSpeed);

            yield return new WaitForSeconds(timeBetweenBurstShots);
        }
        // Burst finished, clear the coroutine reference
        currentBurstCoroutine = null;
    }
}