using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRocket", menuName = "Armaments/Rockets")]
public class Rockets : ScriptableObject
{
    public new string name;
    public string description;

    public Sprite artwork;
    public GameObject rocketPrefab;
    public GameObject rocketPodPrefab;
    AudioSource Shootsound;

    public int damage;
    public int ammo;
    public float rocketSpeed;
    public float rocketReload;
}
