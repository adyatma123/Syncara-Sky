using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMissile", menuName = "Armaments/Missile")]
public class Missile : ScriptableObject
{
    [Header("Missile Info")]
    [Tooltip("The name of this missile type.")]
    public string missileName = "Homing Missile";
    [Tooltip("A brief description of this missile's purpose.")]
    [TextArea]
    public string missileDescription = "A standard air-to-air missile for engaging aerial targets.";
    public bool multiTargets = false;

    [Header("Missile Components")]
    public Sprite artwork;
    public GameObject missilePrefab;
    public AudioSource Shootsound;
    public AudioSource Explode;

    [Header("Missile Properties")]
    [Tooltip("Missile damage to enemies.")]
    public int damage;
    [Tooltip("Proximity range to enemy takes explosion damage.")]
    public float proximityRadius;
    [Tooltip("Missile maximum overload to turn to the target.")]
    public int rotationSpeed;
    [Tooltip("Missile initial speed after launch.")]
    public float speed;
    [Tooltip("Missile reload time to be used again.")]
    public float reload;
    [Tooltip("The maximum time in seconds the missile will exist before being destroyed.")]
    public int guidanceTime;
    [Tooltip("Ammount of maximum usable missile per slot.")]
    public int maxAmmo;
    [Tooltip("Tier range for the missile to be unlocked.")]
    public int Tier;
    [Tooltip("Missile price per item.")]
    public int Price;

    [Header("Target & Status")]
    [Tooltip("The maximum distance from the missile for it to find and maintain a homing lock.")]
    public float lockRadius = 100f;
    [Tooltip("The maximum angle (in degrees) from the missile's forward direction to acquire a homing lock.")]
    [Range(0, 180)]
    public float maxHomingAngle = 60f;
}
