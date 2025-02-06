using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public Guns guns;
    public Transform bulletSpawnPoint;
    public GameObject bulletPrefab;

    public Aimbot aimbot;

    private float nextFireTime;

    private void Start()
    {
        aimbot.enabled = false;
    }

    void Update()
    {
        {
            if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + 1f / guns.rateOfFire;
                AudioManager.Instance.PlaySFX("GunShoot");
            }
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            aimbot.enabled = !aimbot.enabled; // Enable the Aimbot
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