using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "NewPayload", menuName = "Armaments/Payload")]
public class Payload : ScriptableObject
{
    [Header("Payload Info")]
    [Tooltip("Check this box if this payload is a homing missile. Uncheck for a rocket.")]
    public bool isMissile = true;
    [Tooltip("The name of this payload type.")]
    public string payloadName;
    [Tooltip("A brief description of this payload's purpose.")]
    [TextArea]
    public string payloadDescription;
    [Tooltip("Tier range for the payload to be unlocked.")]
    public int Tier;
    [Tooltip("Payload price per item.")]
    public int Price;

    [Header("Payload Components")]
    public Sprite artwork;
    [Tooltip("The prefab of the projectile to be instantiated.")]
    public GameObject payloadPrefab;
    [Tooltip("The prefab of the rocket pod, if applicable.")]
    public GameObject podPrefab;
    public AudioClip shootSound;
    public AudioClip explodeSound;

    [Header("Payload Stats")]
    [Tooltip("The minimum vehicle tier to equip the payload.")]
    public int tier;
    [Tooltip("The initial speed of the projectile after launch.")]
    public float speed;
    [Tooltip("The damage this payload inflicts on enemies.")]
    public int damage;
    [Tooltip("The time required to reload this payload.")]
    public float reloadTime;
    [Tooltip("The maximum time in seconds the payload will exist before being destroyed.")]
    public int lifeTime;
    [Tooltip("The amount of maximum usable payloads per slot.")]
    public int maxAmmo;

    // --- Missile-Specific Properties ---
    [Header("Missile Properties")]
    [Tooltip("Can this missile track multiple targets simultaneously?")]
    public bool multiTargets = false;
    [Tooltip("Proximity range to an enemy that triggers explosion damage.")]
    public float proximityRadius;
    [Tooltip("Missile maximum overload to turn to the target.")]
    public int rotationSpeed;
    [Tooltip("The maximum distance from the missile for it to find and maintain a homing lock.")]
    public float lockRadius = 100f;
    [Tooltip("The maximum angle (in degrees) from the missile's forward direction to acquire a homing lock.")]
    [Range(0, 180)]
    public float maxHomingAngle = 60f;
}
