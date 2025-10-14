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
}
