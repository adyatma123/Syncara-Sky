using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileController : MonoBehaviour
{
    public Transform[] missileSpawnPoints;
    public GameObject missilePrefab;
    public PlayerController playerCon;
    public Missile missile;
    public float missileReload = 1f; // Delay in seconds
    private int currentMissileIndex = 0;
    private bool isReloading = false;

    public void LaunchMissile()
    {
        if (!isReloading && currentMissileIndex < missileSpawnPoints.Length)
        {
            Transform spawnPoint = missileSpawnPoints[currentMissileIndex];
            GameObject missile = Instantiate(missilePrefab, spawnPoint.position, spawnPoint.rotation);
            missile.transform.Rotate(0f, 180f, 0f); // Rotate 180 degrees around Y-axis. Adjust if necessary.
            AudioManager.Instance.PlaySFX("Missile");

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