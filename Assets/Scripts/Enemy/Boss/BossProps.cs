using UnityEngine;
using System.Collections;

public class BossProps : MonoBehaviour
{
    [Header("Boss Master Stats")]
    public string bossName;
    public int totalBossHealth;
    public int currentBossHealth;

    private EnemyProps[] allParts;

    void Awake()
    {
        allParts = GetComponentsInChildren<EnemyProps>();
        CalculateTotalHealth();
    }

    void CalculateTotalHealth()
    {
        totalBossHealth = 0;
        foreach (var part in allParts)
        {
            // We assume parts are set up via EnemySO
            totalBossHealth += part.enemyDataSource.maxHealth;
        }
        currentBossHealth = totalBossHealth;
    }

    // This can be called by child parts when they take damage
    public void ReportDamage(int amount)
    {
        currentBossHealth -= amount;
        if (currentBossHealth <= 0)
        {
            // Handle Big Explosion / End Level
        }
    }
}