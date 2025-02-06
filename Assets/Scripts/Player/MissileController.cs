using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileController : MonoBehaviour
{
    public Transform[] missileSpawnPoints;
    public GameObject missilePrefab;
    public PlayerController playerCon;
    public int damage;
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
            if (missileScript != null)
            {
                missileScript = NewMethod(missileScript);
                missile.GetComponent<Rigidbody>().velocity = spawnPoint.forward;
            }
            else
            {
                Debug.LogError("HomingMissile component not found on missile!");
            }

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

    private HomingMissile NewMethod(HomingMissile missileScript)
    {
        missileScript.Mdamage = damage; // Set the HomingMissile's damage here!
        return missileScript;
    }
}