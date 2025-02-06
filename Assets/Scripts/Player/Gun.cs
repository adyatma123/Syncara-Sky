using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public Guns guns;
    public Transform bulletSpawnPoint;
    public GameObject bulletPrefab;
    public TextMeshProUGUI overheatText;
    //public Image heatBar;

    public Aimbot aimbot;

    private float nextFireTime;
    public float heatRate; // Heat increase per shot
    public float maxHeat;   // Maximum heat before gun disables
    public float currentHeat = 0f;
    private bool gunOverheated = false; // Track overheat state
    public float blinkDuration = 1f; // Total duration of one blink (fade in + fade out)
    private float blinkTimer = 0f;   // Timer to track the current blink phase
    public float overheatMinCD = 30f;

    private void Start()
    {
        aimbot.enabled = false;
        overheatText.enabled = false;
    }

    void Update()
    {
        if (!gunOverheated) // Only allow shooting if not overheated
        {
            if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + 1f / guns.rateOfFire;
                AudioManager.Instance.PlaySFX("GunShoot");

                // Increase heat
                currentHeat += guns.heatRate;

                // Check for overheat
                if (currentHeat >= maxHeat)
                {
                    gunOverheated = true;
                    overheatText.enabled = true;
                    blinkTimer = 0f; // Reset the blink timer when overheat starts
                    Debug.Log("Gun Overheated!"); // Or display a visual warning
                    // You might want to add a cooldown period here before the gun can fire again
                }
            }
        }

        if (gunOverheated && overheatText != null)
        {
            blinkTimer += Time.deltaTime; // Increment the timer

            // Calculate alpha based on sine wave within the blink duration
            float alpha;
            if (blinkTimer <= blinkDuration / 2f) // Fade In
            {
                alpha = Mathf.Sin((blinkTimer / (blinkDuration / 2f)) * Mathf.PI / 2f); // 0 to 1
            }
            else // Fade Out
            {
                alpha = Mathf.Cos(((blinkTimer - blinkDuration / 2f) / (blinkDuration / 2f)) * Mathf.PI / 2f); // 1 to 0
            }

            Color textColor = overheatText.color;
            textColor.a = alpha; // Set alpha (transparency)
            overheatText.color = textColor;

            // Reset the timer when one blink cycle is complete
            if (blinkTimer >= blinkDuration)
            {
                blinkTimer -= blinkDuration; // or blinkTimer = 0;
            }
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            aimbot.enabled = !aimbot.enabled; // Enable the Aimbot
        }

        // Cool down the gun (even when overheated).
        if (currentHeat > 0)
        {
            currentHeat -= Time.deltaTime * 20f; // Adjust cooldown rate
            currentHeat = Mathf.Max(0, currentHeat); // Prevent heat from going negative.
        }

        if (gunOverheated && currentHeat <= overheatMinCD) // Gun can shoot again.
        {
            gunOverheated = false;
            overheatText.enabled = false;
            // Reset alpha to fully visible when hiding
            Color textColor = overheatText.color;
            textColor.a = 1f;
            overheatText.color = textColor;

            overheatText.enabled = false; // Hide the text
        }

        void Shoot()
        {
            GameObject bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletSpawnPoint.rotation);
            Bullet bulletScript = bullet.GetComponent<Bullet>();
            bulletScript.damage = guns.damage; // Set the bullet's damage to the gun's damage
            bullet.GetComponent<Rigidbody>().velocity = bulletSpawnPoint.forward * guns.bulletSpeed;
        }
    }


}