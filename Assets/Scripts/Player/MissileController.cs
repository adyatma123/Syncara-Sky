using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileController : MonoBehaviour
{
    public Transform[] payloadPoints1;
    public Transform[] payloadPoints2;
    public Transform[] payloadPoints3;
    public GameObject missilePrefab;
    public PlayerController playerCon;
    public Missile missile;
    public float missileReload = 1f; // Delay in seconds
    private int currentMissileIndex = 0;
    private bool isReloading = false;

    public void LaunchMissile()
    {
        if (!isReloading && currentMissileIndex < payloadPoints1.Length)
        {
            Transform spawnPoint = payloadPoints1[currentMissileIndex];
            GameObject missile = Instantiate(missilePrefab, spawnPoint.position, spawnPoint.rotation);
            missile.transform.Rotate(0f, 180f, 0f); // Rotate 180 degrees around Y-axis. Adjust if necessary.
            AudioManager.Instance.PlaySFX("Missile");

            currentMissileIndex++;

            if (currentMissileIndex >= payloadPoints1.Length)
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