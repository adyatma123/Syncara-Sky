using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileController : MonoBehaviour
{
    public Transform[] missileSpawnPoints;
    public GameObject missilePrefab;
    public PlayerController playerCon;
    public int damage = 100;
    public float missilespeed = 100f;
    public float missileReload = 1f; // Delay in seconds

    private int currentMissileIndex = 0;
    private bool isReloading = false;

    public void LaunchMissile()
    {
        if (!isReloading && currentMissileIndex < missileSpawnPoints.Length)
        {
            Transform spawnPoint = missileSpawnPoints[currentMissileIndex];
            GameObject missile = Instantiate(missilePrefab, spawnPoint.position, spawnPoint.rotation);
            HomingMissile missileScript = missile.GetComponent<HomingMissile>();
            missileScript.damage = damage;
            missile.GetComponent<Rigidbody>().velocity = spawnPoint.forward * missilespeed;

            currentMissileIndex++;

            if (currentMissileIndex >= missileSpawnPoints.Length)
            {
                currentMissileIndex = 0;
                isReloading = true;
                StartCoroutine(Reload());
            }
        }

        IEnumerator Reload()
        {
            yield return new WaitForSeconds(missileReload);
            isReloading = false;
        }
    }
}