using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewGun", menuName = "Armaments/Guns")]
public class Guns : ScriptableObject
{
    public new string name;
    public string description;

    public Sprite artwork;
    public Transform bulletSpawnPoint;
    public GameObject bulletPrefab;
    public string ShootSoundKey;

    public int damage;
    public float rateOfFire;
    public float bulletSpeed;
    public float heatRate;
    public int Tier;
    public int Price;

    private void OnValidate()
    {
        damage = Mathf.Clamp(damage, 0, 10000);
        bulletSpeed = Mathf.Max(1f, bulletSpeed);
        rateOfFire = Mathf.Clamp(rateOfFire, 1f, 3000f);
        heatRate = Mathf.Max(0f, heatRate);
        Tier = Mathf.Max(1, Tier);
        Price = Mathf.Max(0, Price);
    }
}
