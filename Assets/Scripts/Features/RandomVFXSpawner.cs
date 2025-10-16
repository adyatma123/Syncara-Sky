using UnityEngine;

/// <summary>
/// Spawns a visual effect prefab at a random position within a box volume 
/// defined by the GameObject's scale and public maximum range multipliers.
/// The spawn area is visualized in the editor using Gizmos.
/// </summary>
public class RandomVFXSpawner : MonoBehaviour
{
    [Header("Spawning Configuration")]
    [Tooltip("The visual effect prefab (e.g., a Particle System or a simple mesh) to spawn.")]
    public GameObject visualEffectPrefab;

    [Tooltip("How often, in seconds, an effect should be spawned.")]
    public float spawnInterval = 0.5f;

    [Tooltip("Optional: Manually assign a Transform to be the parent of the spawned effects. If left empty, the Spawner itself will be the parent.")]
    public Transform parentTransform;

    [Tooltip("Maximum allowed uniform scale for the instantiated prefab. This can be used to constrain the size.")]
    [Range(0.1f, 10.0f)]
    public float maxPrefabScaleConstraint = 1.0f;

    [Header("Spawn Area Multipliers (Gizmo Size)")]
    [Range(0.1f, 5.0f)]
    [Tooltip("Maximum factor for the X-axis range, relative to the object's scale.")]
    public float maxRangeX = 1f;

    [Range(0.1f, 5.0f)]
    [Tooltip("Maximum factor for the Y-axis range, relative to the object's scale.")]
    public float maxRangeY = 1f;

    [Range(0.1f, 5.0f)]
    [Tooltip("Maximum factor for the Z-axis range, relative to the object's scale.")]
    public float maxRangeZ = 1f;

    private float _lastSpawnTime;

    void Start()
    {
        // Check for required setup before starting the spawning loop
        if (visualEffectPrefab == null)
        {
            Debug.LogError("VFX Prefab is not assigned on " + gameObject.name + ". Spawner disabled.");
            enabled = false;
            return;
        }

        if (spawnInterval <= 0)
        {
            Debug.LogWarning("Spawn Interval is set to 0 or less. Defaulting to 1 second.");
            spawnInterval = 1f;
        }

        // Start the repeating spawn function
        InvokeRepeating(nameof(SpawnEffect), 0f, spawnInterval);
    }

    /// <summary>
    /// Calculates a random position within the defined volume and instantiates the prefab.
    /// It then automatically schedules the instance for destruction after its expected lifetime.
    /// </summary>
    private void SpawnEffect()
    {
        // 1. Calculate the scaled full-range (dimension) for each axis.
        float fullRangeX = transform.localScale.x * maxRangeX;
        float fullRangeY = transform.localScale.y * maxRangeY;
        float fullRangeZ = transform.localScale.z * maxRangeZ;

        // 2. Generate random offsets, centered around the GameObject.
        float randomOffsetX = Random.Range(-fullRangeX, fullRangeX);
        float randomOffsetY = Random.Range(-fullRangeY, fullRangeY);
        float randomOffsetZ = Random.Range(-fullRangeZ, fullRangeZ);

        Vector3 randomOffset = new Vector3(randomOffsetX, randomOffsetY, randomOffsetZ);

        // 3. Transform the local offset to world space position relative to the spawner's position
        Vector3 spawnPosition = transform.position + randomOffset;

        // Determine the parent transform: use the assigned parentTransform or fall back to this spawner's transform.
        Transform finalParent = parentTransform != null ? parentTransform : this.transform;

        // 4. Instantiate the prefab and set the spawner object as its parent
        GameObject instance = Instantiate(visualEffectPrefab, spawnPosition, Quaternion.identity, finalParent);

        // NOTE: If you want to enforce the size constraint, you would add the following line here:
        instance.transform.localScale = Vector3.one * maxPrefabScaleConstraint;

        // 5. AUTO-DESTRUCTION LOGIC (Adapted from VisualEffectManager)

        // Try to find a ParticleSystem on the instantiated object
        ParticleSystem ps = instance.GetComponent<ParticleSystem>();

        if (ps != null)
        {
            // Calculate the total duration (main duration + start lifetime)
            float duration = ps.main.duration + ps.main.startLifetimeMultiplier;
            // Add a small buffer just in case
            Destroy(instance, duration + 0.1f);
        }
        else
        {
            // Fallback: Destroy after a fixed time if it's not a ParticleSystem (e.g., a mesh effect)
            // You can adjust this time if your non-ParticleSystem effects need longer.
            Destroy(instance, 3.0f);
        }
    }

    /// <summary>
    /// Draws a yellow wire cube in the editor to visualize the spawn volume.
    /// This only appears in the Scene view, not in the game build.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Ensure the gizmo is drawn correctly using the object's transform
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;

        // Calculate the Gizmo size based on local scale and multipliers
        Vector3 gizmoSize = new Vector3(
            transform.localScale.x * maxRangeX,
            transform.localScale.y * maxRangeY,
            transform.localScale.z * maxRangeZ
        );

        // Draw a wire cube representing the spawn area. 
        // We use Vector3.one to scale the drawing by the size calculated above, which is applied by Gizmos.matrix.
        Gizmos.DrawWireCube(Vector3.zero, gizmoSize);
    }
}
