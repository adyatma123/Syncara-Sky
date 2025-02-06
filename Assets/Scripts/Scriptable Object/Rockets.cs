using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRocket", menuName = "Armaments/Rockets")]
public class Rockets : MonoBehaviour
{
    public new string name;
    public string description;

    public Sprite artwork;
    public Transform bulletSpawnPoint;
    public GameObject bulletPrefab;
    AudioSource Shootsound;

    public int damage;
    public float rocketSpeed;
    public float rocketReload;
}
