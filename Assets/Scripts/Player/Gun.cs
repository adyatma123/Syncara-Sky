using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public Guns guns;
    public Transform[] bulletSpawnPoint1;
    public Transform[] bulletSpawnPoint2;
    public Transform[] bulletSpawnPoint3;
    public ParticleSystem[] muzzleFlashes1;
    public ParticleSystem[] muzzleFlashes2;
    public ParticleSystem[] muzzleFlashes3;
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
    public float gunSpread = 0.1f;
    public int gunStage = 1;
    public int totalGunActive;

    private void Start()
    {
        // Automatically find the TextMeshProUGUI with the "OverheatText" tag
        GameObject overheatTextObject = GameObject.FindWithTag("OverheatText");
        if (overheatTextObject != null)
        {
            overheatText = overheatTextObject.GetComponent<TextMeshProUGUI>();
            if (overheatText != null)
            {
                overheatText.enabled = false;
            }
            else
            {
                Debug.LogError("The GameObject with tag 'OverheatText' does not have a TextMeshProUGUI component.");
            }
        }
        else
        {
            Debug.LogError("No GameObject with tag 'OverheatText' found in the scene.");
        }

        aimbot.enabled = false;
    }

    void Update()
    {
        UpdateActiveGunCount(); // Calculate active guns at start

        if (Input.GetKeyDown(KeyCode.C))
        {
            // Cycle through stages
            gunStage++;
            if (gunStage > 4)
            {
                gunStage = 1; // Reset to stage 1 after stage 4
            }

            Debug.Log("Current Stage: " + gunStage); // Optional: Log the current stage
        }

        if (!gunOverheated) // Only allow shooting if not overheated
        {
            if (Input.GetButton("Gun") && Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + (60f / guns.rateOfFire);
                AudioManager.Instance.PlaySFX("GunShoot");

                // Increase heat
                currentHeat += guns.heatRate * totalGunActive;

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
            UpdateActiveGunCount(); // Update active gun count *before* shooting

            // Stage 1: bulletSpawnPoint1 only
            if (gunStage == 1)
            {
                foreach (Transform spawnPoint in bulletSpawnPoint1)
                {
                    if (spawnPoint.gameObject.activeInHierarchy)
                    {
                        FireBullet(spawnPoint);
                        totalGunActive++;
                    }

                    // Play all muzzle flashes at once (outside the bullet instantiation loop)
                    foreach (ParticleSystem muzzleFlash in muzzleFlashes1)
                    {
                        muzzleFlash.Play();
                    }
                }
            }
            // Stage 2: bulletSpawnPoint2 only
            else if (gunStage == 2)
            {
                foreach (Transform spawnPoint in bulletSpawnPoint2)
                {
                    if (spawnPoint.gameObject.activeInHierarchy)
                    {
                        FireBullet(spawnPoint);
                        totalGunActive++;
                    }
                }

                // Play all muzzle flashes at once (outside the bullet instantiation loop)
                foreach (ParticleSystem muzzleFlash in muzzleFlashes2)
                {
                    muzzleFlash.Play();
                }
            }
            // Stage 3: bulletSpawnPoint1 and bulletSpawnPoint2
            else if (gunStage == 3)
            {
                foreach (Transform spawnPoint in bulletSpawnPoint1)
                {
                    if (spawnPoint.gameObject.activeInHierarchy)
                    {
                        FireBullet(spawnPoint);
                        totalGunActive++;
                    }
                }

                // Play all muzzle flashes at once (outside the bullet instantiation loop)
                foreach (ParticleSystem muzzleFlash in muzzleFlashes1)
                {
                    muzzleFlash.Play();
                }

                foreach (Transform spawnPoint in bulletSpawnPoint2)
                {
                    if (spawnPoint.gameObject.activeInHierarchy)
                    {
                        FireBullet(spawnPoint);
                        totalGunActive++;
                    }
                }

                // Play all muzzle flashes at once (outside the bullet instantiation loop)
                foreach (ParticleSystem muzzleFlash in muzzleFlashes2)
                {
                    muzzleFlash.Play();
                }
            }
            // Stage 4: bulletSpawnPoint1, bulletSpawnPoint2, and bulletSpawnPoint3
            else if (gunStage == 4)
            {
                foreach (Transform spawnPoint in bulletSpawnPoint1)
                {
                    if (spawnPoint.gameObject.activeInHierarchy)
                    {
                        FireBullet(spawnPoint);
                        totalGunActive++;
                    }
                }

                // Play all muzzle flashes at once (outside the bullet instantiation loop)
                foreach (ParticleSystem muzzleFlash in muzzleFlashes1)
                {
                    muzzleFlash.Play();
                }

                foreach (Transform spawnPoint in bulletSpawnPoint2)
                {
                    if (spawnPoint.gameObject.activeInHierarchy)
                    {
                        FireBullet(spawnPoint);
                        totalGunActive++;
                    }
                }

                // Play all muzzle flashes at once (outside the bullet instantiation loop)
                foreach (ParticleSystem muzzleFlash in muzzleFlashes2)
                {
                    muzzleFlash.Play();
                }

                foreach (Transform spawnPoint in bulletSpawnPoint3)
                {
                    if (spawnPoint.gameObject.activeInHierarchy)
                    {
                        FireBullet(spawnPoint);
                        totalGunActive++;
                    }
                }

                // Play all muzzle flashes at once (outside the bullet instantiation loop)
                foreach (ParticleSystem muzzleFlash in muzzleFlashes3)
                {
                    muzzleFlash.Play();
                }
            }
        }
    }

    // Helper function to fire a bullet (reduces code duplication)
    private void FireBullet(Transform spawnPoint)
    {
        // Calculate random X rotation offset
        float randomXRotation = Random.Range(-gunSpread, gunSpread);

        // Create a new rotation by adding the random offset to the existing X rotation
        Quaternion bulletRotation = spawnPoint.rotation;
        bulletRotation *= Quaternion.Euler(0f, randomXRotation, 0f); // Add rotation around X-axis

        GameObject bulletInstance = Instantiate(guns.bulletPrefab, spawnPoint.position, spawnPoint.rotation);
        Bullet bulletScript = bulletInstance.GetComponent<Bullet>();
        bulletScript.damage = guns.damage;
        bulletInstance.GetComponent<Rigidbody>().velocity = bulletRotation * Vector3.forward * guns.bulletSpeed; // Apply rotation to velocity
    }

    private void UpdateActiveGunCount()
    {
        totalGunActive = 0; // Reset count
        /*foreach (Transform spawnPoint in bulletSpawnPoint2)
        {
            if (spawnPoint.gameObject.activeInHierarchy)
            {
                totalGunActive++;
            }
        }*/
    }


}