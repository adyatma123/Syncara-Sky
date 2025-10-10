using UnityEngine;
using System.Collections;

/// <summary>
/// Handles the Homing Missile (MSL) firing logic for the enemy.
/// This uses a coroutine for delayed, repeating fire.
/// </summary>
public class EnemyMSL : MonoBehaviour
{
    [Tooltip("The missile prefab to instantiate.")]
    public GameObject missilePrefab;

    [Header("Missile Timing")]
    [Tooltip("Initial delay before the first missile launches after activation (in seconds).")]
    public float initialMissileLaunchDelay = 1.0f;

    [Header("Weapon Setup")]
    [Tooltip("The Transform from where projectiles will be instantiated.")]
    public Transform firePoint;

    private EnemyProps enemyProps;
    private bool isShooting = false;
    private Coroutine missileLaunchCoroutine;

    void Awake()
    {
        enemyProps = GetComponent<EnemyProps>();
        if (enemyProps == null)
        {
            Debug.LogError("EnemyMSLShoot requires an EnemyProps component on the parent GameObject.", this);
            enabled = false;
        }

        if (firePoint == null)
        {
            Debug.LogError("EnemyMSLShoot requires a Fire Point Transform assigned in the Inspector.", this);
            enabled = false;
        }
    }

    public void Activate()
    {
        if (!enemyProps.IsArmedMSL) return;
        if (isShooting) return;
        if (missilePrefab == null)
        {
            Debug.LogWarning($"MSL ARMED but missilePrefab is NULL on {gameObject.name}. Cannot fire MSL.");
            return;
        }

        isShooting = true;
        Debug.Log($"[{gameObject.name}] MSL ACTIVATED shooting.");

        if (missileLaunchCoroutine != null) StopCoroutine(missileLaunchCoroutine);
        missileLaunchCoroutine = StartCoroutine(LaunchMissileRoutine());
    }

    public void Deactivate()
    {
        if (!isShooting) return;

        isShooting = false;
        Debug.Log($"[{gameObject.name}] MSL DEACTIVATED shooting.");

        if (missileLaunchCoroutine != null)
        {
            StopCoroutine(missileLaunchCoroutine);
            missileLaunchCoroutine = null;
        }
    }

    private void ShootMissile()
    {
        // Instantiate the missile at the firePoint's position and rotation
        GameObject instantiatedProjectile = Instantiate(missilePrefab, firePoint.position, firePoint.rotation);

        // Try to get the homing missile script
        EnemyHomingMissile missileScript = instantiatedProjectile.GetComponent<EnemyHomingMissile>();

        if (missileScript != null)
        {
            // Initialize homing missile properties
            missileScript.damage = (int)enemyProps.EnemyDmg;
            missileScript.owner = this.gameObject;
            Debug.Log($"[{gameObject.name}] Missile launched at {Time.time}!");
        }
        else
        {
            Debug.LogWarning($"Instantiated MSL prefab {missilePrefab.name} is missing the EnemyHomingMissile script.");
        }

        // Example of playing an SFX:
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("Player Missile");
        }
    }

    private IEnumerator LaunchMissileRoutine()
    {
        Debug.Log($"[{gameObject.name}] Missile Launch Coroutine STARTED. Initial delay of {initialMissileLaunchDelay} seconds.");

        // 1. Initial delay before the first missile is launched
        yield return new WaitForSeconds(initialMissileLaunchDelay);

        Debug.Log($"Missile routine: Initial delay complete at {Time.time}. Starting continuous loop.");

        // Convert fire rate (BPM) to delay (seconds)
        float delayBetweenMissiles = (60f / enemyProps.FireRate);

        // 2. Continuous firing loop
        while (isShooting && enemyProps.IsArmedMSL && missilePrefab != null)
        {
            ShootMissile();

            // Wait for the fire rate duration before checking again
            yield return new WaitForSeconds(delayBetweenMissiles);
        }
        Debug.Log($"Missile routine: Loop stopped (isShooting or IsArmedMSL is false).");
    }
}
