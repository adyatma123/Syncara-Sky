using System;
using UnityEngine;

public class BossWeakpoint : MonoBehaviour
{
    [Header("Data Source")]
    public EnemyData data;

    [Header("Phase Behavior")]
    public bool hittableOutsidePhase = false;

    [Header("Runtime")]
    public bool isActive = false;

    private int currentHealth;

    private BossPhaseController boss;
    private BossMG bossMG;
    private Collider[] colliders;

    void Awake()
    {
        boss = GetComponentInParent<BossPhaseController>();
        bossMG = GetComponent<BossMG>();
        colliders = GetComponentsInChildren<Collider>(true);

        if (data != null)
            currentHealth = data.maxHealth;
    }

    public void TakeDamage(int damage)
    {
        // ❌ not active AND not hittable → ignore completely
        if (!isActive && !hittableOutsidePhase) return;

        // 🔥 outside phase = can hit but NOT destroy
        if (!isActive && hittableOutsidePhase)
        {
            Debug.Log($"[BossWeakpoint] {name} HIT (LOCKED PHASE)");
            return;
        }

        // ✅ normal damage (active phase only)
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            DestroyWeakpoint();

            EnemyProps.ReportEnemyDestroyed(data.scoreVal);
        }
    }

    void DestroyWeakpoint()
    {
        isActive = false;

        // Spawn the effect at the enemy's position and current rotation
        VisualEffectManager.Instance.PlayEffect("Aircraft Explode", transform.position, transform.rotation);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("Explode");
        }

        SetColliders(false);

        if (bossMG != null)
            bossMG.Deactivate();

        gameObject.SetActive(false);

        if (boss != null)
            boss.OnWeakpointDestroyed(this);
    }

    public void Activate()
    {
        isActive = true;

        if (data != null)
            currentHealth = data.maxHealth;

        SetColliders(true);

        if (bossMG != null && data.isArmedMG)
            bossMG.Activate();
    }

    public void Deactivate()
    {
        isActive = false;

        // 🔥 KEY LOGIC HERE
        if (hittableOutsidePhase)
        {
            SetColliders(true);  // still hittable
        }
        else
        {
            SetColliders(false); // completely disabled
        }

        if (bossMG != null)
            bossMG.Deactivate();
    }

    void SetColliders(bool state)
    {
        foreach (var col in colliders)
        {
            col.enabled = state;
        }
    }

    // expose data
    public float FireRate => data != null ? data.fireRate : 0f;
    public float BulletSpeed => data != null ? data.bulletSpeed : 0f;
    public int Damage => data != null ? data.enemyDmg : 0;

    public int Score => data != null ? data.scoreVal : 0;
}