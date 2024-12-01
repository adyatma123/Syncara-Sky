using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public Transform bulletSpawnPoint;
    public GameObject bulletPrefab;
    public float rateOfFire;
    public int damage;
    AudioSource Shootsound;

    public Aimbot aimbot;

    private float nextFireTime;
    public float bulletSpeed = 1000;

    private void Start()
    {
        aimbot.enabled = false;
        Shootsound = GetComponent<AudioSource>();
    }

    void Update()
    {
        {
            if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
            {
                Shoot();
                nextFireTime = Time.time + 1f / rateOfFire;
                Shootsound.Play();
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
            bulletScript.damage = damage; // Set the bullet's damage to the gun's damage
            bullet.GetComponent<Rigidbody>().velocity = bulletSpawnPoint.forward * bulletSpeed;
        }
    }
}