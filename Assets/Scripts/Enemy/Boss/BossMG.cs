using UnityEngine;
using System.Collections;

public class BossMG : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform[] firePoints;

    private BossWeakpoint weakpoint;

    private bool isShooting = false;
    private float nextFireTime;

    void Awake()
    {
        weakpoint = GetComponent<BossWeakpoint>();

        if (firePoints == null || firePoints.Length == 0)
        {
            Debug.LogError($"[BossMG] No fire points on {name}");
            enabled = false;
        }
    }

    public void Activate()
    {
        if (weakpoint == null || weakpoint.data == null) return;

        isShooting = true;
        nextFireTime = Time.time;

        Debug.Log($"[BossMG] Activated on {name}");
    }

    public void Deactivate()
    {
        isShooting = false;
    }

    void Update()
    {
        if (!isShooting || weakpoint == null || !weakpoint.isActive) return;

        float fireInterval = 60f / weakpoint.FireRate;

        if (Time.time >= nextFireTime)
        {
            Fire();

            nextFireTime = Time.time + fireInterval;
        }
    }

    void Fire()
    {
        foreach (Transform fp in firePoints)
        {
            GameObject bullet = Instantiate(bulletPrefab, fp.position, fp.rotation);

            EnemyBullet bulletScript = bullet.GetComponent<EnemyBullet>();
            if (bulletScript != null)
            {
                bulletScript.damage = weakpoint.Damage;
                bulletScript.owner = gameObject;
                bulletScript.SetDirectionAndSpeed(fp.forward, weakpoint.BulletSpeed);
            }
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("Enemy Shoot");
        }
    }
}