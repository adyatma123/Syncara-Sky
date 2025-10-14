using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Structure to hold a single Visual Effect prefab and its unique identifier (name).
/// This is used for easy assignment in the Unity Inspector.
/// </summary>
[System.Serializable]
public struct VisualEffectEntry
{
    [Tooltip("The unique name used to call this effect (e.g., 'MissileExplosion', 'BulletHit').")]
    public string effectName;
    [Tooltip("The actual GameObject prefab containing the Particle System or Visual Effect.")]
    public GameObject prefab;
}

/// <summary>
/// A Singleton class responsible for managing and spawning all visual effects (VFX) in the game.
/// Effects are stored in a Dictionary for fast lookup by name.
/// </summary>
public class VisualEffectManager : MonoBehaviour
{
    // Singleton pattern
    public static VisualEffectManager Instance { get; private set; }

    [Header("Available Visual Effects")]
    [Tooltip("Assign all VFX prefabs and their corresponding names here.")]
    [SerializeField]
    private VisualEffectEntry[] availableEffects;

    // Dictionary for fast lookup of prefabs by their name
    private Dictionary<string, GameObject> effectLookupTable;

    private void Awake()
    {
        // Enforce Singleton
        if (Instance == null)
        {
            Instance = this;
            // Optionally use DontDestroyOnLoad if the manager should persist across scenes
            // DontDestroyOnLoad(gameObject);
            InitializeLookupTable();
        }
        else
        {
            // Destroy this instance if another one already exists
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Populates the internal Dictionary from the Inspector array for O(1) lookup.
    /// </summary>
    private void InitializeLookupTable()
    {
        effectLookupTable = new Dictionary<string, GameObject>();

        if (availableEffects == null) return;

        foreach (var entry in availableEffects)
        {
            if (string.IsNullOrEmpty(entry.effectName) || entry.prefab == null)
            {
                Debug.LogWarning("VisualEffectManager: Found an effect entry with a missing name or prefab. Skipping.", this);
                continue;
            }

            // Ensure unique names
            if (effectLookupTable.ContainsKey(entry.effectName))
            {
                Debug.LogError($"VisualEffectManager: Duplicate effect name detected: '{entry.effectName}'. Please use unique names.", this);
                continue;
            }

            effectLookupTable.Add(entry.effectName, entry.prefab);
        }
        Debug.Log($"VisualEffectManager Initialized with {effectLookupTable.Count} unique effects.");
    }

    /// <summary>
    /// PUBLIC: Instantiates a visual effect prefab by its registered name at a specified world position and rotation.
    /// </summary>
    /// <param name="effectName">The unique name of the effect (must match an entry in the Inspector).</param>
    /// <param name="position">The world position to spawn the effect.</param>
    /// <param name="rotation">The world rotation for the effect.</param>
    /// <returns>The instantiated GameObject, or null if the effect name was not found.</returns>
    public GameObject PlayEffect(string effectName, Vector3 position, Quaternion rotation)
    {
        if (effectLookupTable == null || !effectLookupTable.ContainsKey(effectName))
        {
            Debug.LogWarning($"VisualEffectManager: Attempted to play unknown effect: '{effectName}'.", this);
            return null;
        }

        GameObject effectPrefab = effectLookupTable[effectName];

        // Instantiate the effect
        GameObject instance = Instantiate(effectPrefab, position, rotation);

        // --- IMPORTANT: Ensure the effect instance self-destructs after completion ---
        // This relies on the VFX prefab having a script (e.g., AutoDestroy) attached,
        // or being a Particle System with 'Stop Action' set to 'Destroy'.

        // If it's a Particle System, we can detect its duration to destroy it:
        ParticleSystem ps = instance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            float duration = ps.main.duration + ps.main.startLifetimeMultiplier;
            Destroy(instance, duration);
        }
        else
        {
            // Fallback: Destroy after a fixed time if it's not a ParticleSystem (adjust time as needed)
            Destroy(instance, 3.0f);
        }

        return instance;
    }

    /// <summary>
    /// Overload to play an effect without specifying rotation (uses Quaternion.identity).
    /// </summary>
    public GameObject PlayEffect(string effectName, Vector3 position)
    {
        return PlayEffect(effectName, position, Quaternion.identity);
    }
}
