using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRocket", menuName = "Armaments/Rockets")]
public class Rockets : ScriptableObject
{
    [Header("Rocket Info")]
    [Tooltip("The name of this missile type.")]
    public string missileName = "Rocket";
    [Tooltip("A brief description of this rocket's purpose.")]
    [TextArea]
    public string missileDescription = "A standard unguided rocket for engaging targets.";

    [Header("Rocket Components")]
    public Sprite artwork;
    public GameObject rocketPrefab;
    public GameObject podPrefab;
    public AudioSource Shootsound;
    public AudioSource Explode;

    [Header("Rocket Properties")]
    [Tooltip("Rocket damage to enemies.")]
    public int damage;
    [Tooltip("Rocket initial speed after launch.")]
    public float speed;
    [Tooltip("Rocket reload time to be used again.")]
    public float reload;
    [Tooltip("The maximum time in seconds the rocket will exist before being destroyed.")]
    public int lifeTime;
    [Tooltip("Ammount of maximum usable Rocket per slot.")]
    public int maxAmmo;
    [Tooltip("Tier range for the Rocket to be unlocked.")]
    public int Tier;
    [Tooltip("Rocket price per item.")]
    public int Price;
}
