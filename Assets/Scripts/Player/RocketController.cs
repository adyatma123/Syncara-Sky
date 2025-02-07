using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketController : MonoBehaviour
{
    public Transform[] payloadPoints1;
    public Transform[] payloadPoints2;
    public Transform[] payloadPoints3;
    public PlayerController playerCon;
    public Rockets rockets;
    public int ammo;
    private int currentRocketIndex = 0;

    public void LaunchRocket()
    {
        if (ammo > 0 && rockets != null && rockets.rocketPrefab != null) // Check ammo and nulls
        {
            if (currentRocketIndex >= payloadPoints2.Length)
            {
                currentRocketIndex = 0; // Reset index to loop back
            }

            Transform spawnPoint = payloadPoints2[currentRocketIndex];

            GameObject rocketInstance = Instantiate(rockets.rocketPrefab, spawnPoint.position, spawnPoint.rotation);

            Transform rocketTransform = rocketInstance.transform;

            rocketTransform.Rotate(0f, 180f, 0f);
            AudioManager.Instance.PlaySFX("Rocket");

            currentRocketIndex++;
            ammo--; // Decrease ammo after each shot

            Debug.Log("Ammo remaining: " + ammo); // Optional: Display ammo count
        }
    }

}