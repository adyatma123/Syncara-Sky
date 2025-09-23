using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileController : MonoBehaviour
{
    public Transform[] payloadPoints1;
    public Transform[] payloadPoints2;
    public Transform[] payloadPoints3;
    public AircraftController playerCon;
    public Payload payload;
    public float missileReload = 1f; // Delay in seconds
    private int currentMissileIndex = 0;
    private bool isReloading = false;

    public void LaunchMissile()
    {
        if (!isReloading && currentMissileIndex < payloadPoints3.Length)
        {
            Transform spawnPoint = payloadPoints3[currentMissileIndex];
            GameObject missileInstance = Instantiate(payload.payloadPrefab, spawnPoint.position, spawnPoint.rotation);

            // Get the transform of the INSTANTIATED GameObject
            Transform missileTransform = missileInstance.transform; // This is the correct way!

            missileTransform.Rotate(0f, 180f, 0f); // Rotate 180 degrees around Y-axis. Adjust if necessary.
            AudioManager.Instance.PlaySFX("Missile");

            currentMissileIndex++;

            if (currentMissileIndex >= payloadPoints3.Length)
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