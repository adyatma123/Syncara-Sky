using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMissile", menuName = "Armaments/Missile")]
public class Missile : ScriptableObject
{
    public new string name;
    public string description;

    public Sprite artwork;
    public GameObject missilePrefab;
    public AudioSource Shootsound;

    public int damage;
    public int ammo;
    public float missileSpeed;
    public float missileReload;
}
