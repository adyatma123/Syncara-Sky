using System.Collections;
using UnityEngine;
using System; // Required for Action events

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

    [Header("Story Integration (Optional)")]
    [Tooltip("The 0-based index of the Story Checkpoint that MUST be triggered before this wave can start. Set to -1 to ignore.")]
    public int requiredCheckpointIndex = -1; // NEW FIELD
}

/// <summary>
/// Manages the sequential spawning of enemy waves. It delegates Level Completion to the GameManager.
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    // --- SINGLETON IMPLEMENTATION ---
    public static WaveSpawner Instance { get; private set; }
    // --------------------------------

    // --- EVENT: Fires when a wave is cleared, passing the index of the CLEARED wave ---
    public event Action<int> OnWaveCleared;
    // --------------------------------------------------------------------------------------

    [Header("Wave Configuration")]
    [Tooltip("List of waves to spawn in order.")]
    public Wave[] waves;

    // The current wave instance being monitored
    private GameObject currentWaveInstance = null;

    // FIX: Changed from private to public so the Debug Overlay can access the current wave index.
    public int waveIndex = 0;

    // Reference to the coroutine so we can stop it for a restart
    private Coroutine spawnWavesCoroutine;

    // NEW: Variabel untuk melacak status Story Checkpoint
    private bool[] checkpointTriggerStatus;

    void Awake()
    {
        // --- SINGLETON SETUP ---
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        // -------------------------

        // Initialize status array
        if (StoryEventManager.Instance != null && StoryEventManager.Instance.storyCheckpoints.Length > 0)
        {
            checkpointTriggerStatus = new bool[StoryEventManager.Instance.storyCheckpoints.Length];
        }

    }

    void Start()
    {
        if (waves.Length == 0)
        {
            Debug.LogWarning("No waves defined in the spawner.");
            // Tell the GameManager that the level is technically complete
            if (GameManager.Instance != null)
            {
                GameManager.Instance.NotifyAllWavesCompleted();
            }
            return;
        }

        // Subscribe to StoryEventManager events if available
        if (StoryEventManager.Instance != null)
        {
            StoryEventManager.Instance.OnCheckpointTriggered += OnStoryCheckpointTriggered;
        }

        // Begin the initial spawning sequence
        spawnWavesCoroutine = StartCoroutine(SpawnAllWaves());
    }

    void OnDestroy()
    {
        if (StoryEventManager.Instance != null)
        {
            StoryEventManager.Instance.OnCheckpointTriggered -= OnStoryCheckpointTriggered;
        }
    }

    /// <summary>
    /// Callback method for the StoryEventManager event.
    /// </summary>
    private void OnStoryCheckpointTriggered(int checkpointIndex)
    {
        if (checkpointIndex >= 0 && checkpointIndex < checkpointTriggerStatus.Length)
        {
            checkpointTriggerStatus[checkpointIndex] = true;
            Debug.Log($"[WaveSpawner] Checkpoint {checkpointIndex} triggered. Checking for pending waves...");
        }
    }


    /// <summary>
    /// Checks for the debug key press to restart the wave sequence.
    /// </summary>
    void Update()
    {
        // Check for Numpad 3 key press to restart the waves
        if (Input.GetKeyDown(KeyCode.Keypad3))
        {
            RestartWaveSequence();
        }

        // Removed EndScene check as it is now in GameManager
    }

    /// <summary>
    /// Resets the wave sequence and clears UI status.
    /// </summary>
    void RestartWaveSequence()
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

        // Reset Checkpoint status
        if (checkpointTriggerStatus != null)
        {
            for (int i = 0; i < checkpointTriggerStatus.Length; i++)
            {
                checkpointTriggerStatus[i] = false;
            }
        }


        // Reset level completion state in GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetLevelState();
        }

        // 4. Start the sequence again
        spawnWavesCoroutine = StartCoroutine(SpawnAllWaves());
    }

    /// <summary>
    /// Coroutine to handle the entire wave spawning process sequentially.
    /// </summary>
    IEnumerator SpawnAllWaves()
    {
        Transform spawnerTransform = this.transform;

        for (waveIndex = 0; waveIndex < waves.Length; waveIndex++)
        {
            Wave wave = waves[waveIndex];

            // NEW LOGIC: TUNGGU CHECKPOINT SEBELUM MEMULAI WAVE
            if (wave.requiredCheckpointIndex != -1)
            {
                int reqIndex = wave.requiredCheckpointIndex;
                if (StoryEventManager.Instance != null && checkpointTriggerStatus != null && reqIndex < checkpointTriggerStatus.Length)
                {
                    Debug.Log($"[WaveSpawner] Waiting for Story Checkpoint {reqIndex} to be triggered before starting Wave {waveIndex + 1}...");

                    // Tunggu sampai status checkpoint berubah menjadi true
                    yield return new WaitUntil(() => checkpointTriggerStatus[reqIndex]);

                    Debug.Log($"[WaveSpawner] Story Checkpoint {reqIndex} triggered. Starting Wave {waveIndex + 1}.");
                }
                else if (StoryEventManager.Instance == null)
                {
                    Debug.LogWarning($"[WaveSpawner] Wave {waveIndex + 1} requires Checkpoint {reqIndex}, but StoryEventManager is missing. Ignoring requirement.");
                }
                else
                {
                    Debug.LogWarning($"[WaveSpawner] Wave {waveIndex + 1} requires Checkpoint {reqIndex}, but index is invalid. Ignoring requirement.");
                }
            }


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
            Debug.Log($"Monitoring Wave. Waiting for all enemies to be cleared...");
            yield return new WaitUntil(() =>
            {
                // Check if the instance is still valid and has no children.
                return currentWaveInstance == null || currentWaveInstance.transform.childCount == 0;
            });

            // --- FIRE EVENT AFTER WAVE IS CLEARED ---
            OnWaveCleared?.Invoke(waveIndex + 1); // Pass the 1-based index of the cleared wave
            // ----------------------------------------

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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.NotifyAllWavesCompleted();
        }
        spawnWavesCoroutine = null; // Clear the coroutine reference as it is finished
    }

    // Optional: Add a public method to check if spawning is finished
    public bool IsSpawningFinished()
    {
        return waveIndex >= waves.Length && currentWaveInstance == null;
    }

    public int GetTotalEnemyCount()
    {
        int total = 0;

        foreach (var wave in waves)
        {
            if (wave.waveContainerPrefab != null)
            {
                total += wave.waveContainerPrefab.GetComponentsInChildren<EnemyProps>(true).Length;
            }
        }

        return total;
    }
}
