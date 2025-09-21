using UnityEngine;
using System.Collections;

public class AIShoot : MonoBehaviour
{
    [Tooltip("The bullet prefab to instantiate.")]
    public GameObject bulletPrefab;

    [Header("Burst Fire Settings")]
    [Tooltip("Check to enable 3-round burst fire instead of a single shot.")]
    public bool useBurstFire = false;
    [Tooltip("The time (in seconds) between each shot in a burst.")]
    public float timeBetweenBurstShots = 0.1f;

    [Header("Weapon Setup")]
    [Tooltip("The Transform from where bullets will be instantiated. Can be a child GameObject.")]
    public Transform firePoint;

    private EnemyProps enemyProps;
    private float nextFireTime;
    private bool isShooting = true;

    void Awake()
    {
        // Get a reference to the EnemyProps component from the PARENT GameObject
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
    }

    public void Activate()
    {
        isShooting = true;

        if (enemyProps.FireRate > 0)
        {
            nextFireTime = Time.time + (60f / enemyProps.FireRate);
        }
        else
        {
            nextFireTime = Time.time + 1f;
        }
    }

    public void Deactivate()
    {
        isShooting = false;
    }

    void Update()
    {
        if (isShooting && Time.time >= nextFireTime)
        {
            if (useBurstFire)
            {
                StartCoroutine(FireBurst());
            }
            else
            {
                ShootSingle();
            }

            nextFireTime = Time.time + (60f / enemyProps.FireRate);
        }
    }

    private void ShootSingle()
    {
        ShootBullet();
    }

    private IEnumerator FireBurst()
    {
        for (int i = 0; i < 3; i++)
        {
            ShootBullet();
            yield return new WaitForSeconds(timeBetweenBurstShots);
        }
    }

    private void ShootBullet()
    {
        if (bulletPrefab != null)
        {
            // Instantiate the bullet at the firePoint's position and rotation
            GameObject instantiatedBullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
            EnemyBullet bulletScript = instantiatedBullet.GetComponent<EnemyBullet>();

            if (bulletScript != null)
            {
                bulletScript.damage = (int)enemyProps.EnemyDmg;
                bulletScript.owner = this.gameObject;
                Vector3 shootDirection = firePoint.forward;
                bulletScript.SetDirectionAndSpeed(shootDirection, enemyProps.BulletSpeed);
            }
            else
            {
                Debug.LogWarning("Instantiated bullet prefab does not have an 'EnemyBullet' script attached.");
            }
        }
        else
        {
            Debug.LogWarning("Cannot shoot: bulletPrefab is null.");
        }
    }
}
