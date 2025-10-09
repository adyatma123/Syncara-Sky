using System.Collections;
using UnityEngine;

/// <summary>
/// Serializable class to define a single wave configuration.
/// This will appear in the Unity Inspector.
/// </summary>
[System.Serializable]
public class Wave
{
    [Tooltip("The parent GameObject prefab containing all the enemy prefabs.")]
    public GameObject waveContainerPrefab;

    [Tooltip("The time delay (in seconds) to wait AFTER this wave has been cleared, but BEFORE the next wave is spawned.")]
    public float delayAfterClear = 1f;
}

/// <summary>
/// Manages the sequential spawning of enemy waves.
/// Waves are spawned one after another, waiting until all enemies (children) 
/// in the current wave are destroyed, followed by a delay before starting the next wave.
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    [Header("Wave Configuration")]
    [Tooltip("List of waves to spawn in order.")]
    public Wave[] waves;

    [Header("Runtime Status")]
    // The current wave instance being monitored
    private GameObject currentWaveInstance = null;
    private int waveIndex = 0;
    
    // Reference to the coroutine so we can stop it for a restart
    private Coroutine spawnWavesCoroutine;

    void Start()
    {
        // The spawner now uses its own Transform for instantiation, so we only check the waves list.
        if (waves.Length == 0)
        {
            Debug.LogWarning("No waves defined in the spawner.");
            return;
        }

        // Begin the initial spawning sequence
        spawnWavesCoroutine = StartCoroutine(SpawnAllWaves());
    }
    
    /// <summary>
    /// Checks for the debug key press to restart the wave sequence.
    /// </summary>
    void Update()
    {
        // Check for Numpad 1 key press to restart the waves
        if (Input.GetKeyDown(KeyCode.Keypad3))
        {
            Debug.Log("------------------------------------");
            Debug.Log("Numpad 3 Pressed: RESTARTING wave sequence from Wave 1.");
            Debug.Log("------------------------------------");

            // 1. Stop the current coroutine if it's running
            if (spawnWavesCoroutine != null)
            {
                StopCoroutine(spawnWavesCoroutine);
                spawnWavesCoroutine = null; // Clear the reference
            }

            // 2. Clean up the current wave if one exists in the scene
            if (currentWaveInstance != null)
            {
                Destroy(currentWaveInstance);
                currentWaveInstance = null;
            }

            // 3. Reset state variables
            waveIndex = 0;
            
            // 4. Start the sequence again
            spawnWavesCoroutine = StartCoroutine(SpawnAllWaves());
        }
    }

    /// <summary>
    /// Coroutine to handle the entire wave spawning process sequentially.
    /// </summary>
    IEnumerator SpawnAllWaves()
    {
        // Store the spawner's transform details once for cleaner code
        Transform spawnerTransform = this.transform;

        // Loop through all defined waves
        for (waveIndex = 0; waveIndex < waves.Length; waveIndex++)
        {
            Wave wave = waves[waveIndex];

            // 1. Log the start of the wave
            Debug.Log($"--- Starting Wave {waveIndex + 1}/{waves.Length} ---");

            // 2. Instantiate the Wave Container Prefab at the spawner's location
            currentWaveInstance = Instantiate(
                wave.waveContainerPrefab,
                spawnerTransform.position, // Using the spawner's position
                spawnerTransform.rotation // Using the spawner's rotation
            );

            // Set the name for clarity in the Hierarchy
            currentWaveInstance.name = $"WAVE_CONTAINER_{waveIndex + 1}";

            // 3. Wait until the current wave instance has no children left (enemies defeated).
            Debug.Log($"Monitoring Wave. Waiting for all {currentWaveInstance.transform.childCount} enemies to be cleared...");
            yield return new WaitUntil(() => 
            {
                // Check if the instance is still valid and has no children.
                return currentWaveInstance == null || currentWaveInstance.transform.childCount == 0;
            });

            // 4. Clean up: Destroy the empty container.
            if (currentWaveInstance != null)
            {
                Debug.Log($"Wave {waveIndex + 1} cleared. Destroying container.");
                Destroy(currentWaveInstance);
            }
            currentWaveInstance = null;
            
            // 5. Wait for the inter-wave delay before starting the next wave.
            if (waveIndex < waves.Length - 1) // Wait only if there is a next wave
            {
                Debug.Log($"Inter-Wave Delay: Waiting for {wave.delayAfterClear} seconds before spawning Wave {waveIndex + 2}...");
                yield return new WaitForSeconds(wave.delayAfterClear);
            }
        }

        // 6. All waves complete
        Debug.Log("--- All Waves Complete! ---");
        spawnWavesCoroutine = null; // Clear the coroutine reference as it is finished
    }

    // Optional: Add a public method to check if spawning is finished
    public bool IsSpawningFinished()
    {
        return waveIndex >= waves.Length && currentWaveInstance == null;
    }
}
