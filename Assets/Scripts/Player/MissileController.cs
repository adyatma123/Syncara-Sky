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

    public void LaunchMissile()
    {
        if (currentMissileIndex < missileSpawnPoints.Length)
        {
            Transform spawnPoint = missileSpawnPoints[currentMissileIndex];
            GameObject missile = Instantiate(missilePrefab, spawnPoint.position, spawnPoint.rotation);
            HomingMissile missileScript = missile.GetComponent<HomingMissile>();
            missileScript.damage = damage;
            missileScript.lockRadius = playerCon.lockRadius;
            missile.GetComponent<Rigidbody>().velocity = spawnPoint.forward * missilespeed;

            currentMissileIndex++;

            if (currentMissileIndex >= missileSpawnPoints.Length)
            {
                StartCoroutine(ResetMissileIndex());
            }
        }

        IEnumerator ResetMissileIndex()
        {
            yield return new WaitForSeconds(missileReload);
            currentMissileIndex = 0;
        }
    }
}