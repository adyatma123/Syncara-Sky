using System.Collections;
using UnityEngine;
using UnityEngine.UI; // Required for legacy Text component (or TextMeshPro)
using UnityEngine.SceneManagement; // Required for loading scenes

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
/// Manages the sequential spawning of enemy waves and handles the Level Complete state.
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    [Header("Wave Configuration")]
    [Tooltip("List of waves to spawn in order.")]
    public Wave[] waves;

    [Header("Level Completion UI")]
    [Tooltip("The UI Text or GameObject to display when all waves are complete.")]
    public GameObject completionUIObject; // Use GameObject for flexibility (Text, TextMeshPro, Panel, etc.)

    [Header("End Scene Settings")]
    [Tooltip("The name of the scene to load when the player presses SPACE after completion. Leave empty to quit the application.")]
    public string nextSceneName = "MainMenu";

    // The current wave instance being monitored
    private GameObject currentWaveInstance = null;
    private int waveIndex = 0;

    // Reference to the coroutine so we can stop it for a restart
    private Coroutine spawnWavesCoroutine;

    private bool isLevelComplete = false;

    void Awake()
    {
        // Ensure UI is disabled at the start of the scene
        if (completionUIObject != null)
        {
            completionUIObject.SetActive(false);
        }
    }

    void Start()
    {
        // The spawner now uses its own Transform for instantiation, so we only check the waves list.
        if (waves.Length == 0)
        {
            Debug.LogWarning("No waves defined in the spawner.");
            // Immediately flag completion if there are no waves
            HandleAllWavesCompleted();
            return;
        }

        // Begin the initial spawning sequence
        spawnWavesCoroutine = StartCoroutine(SpawnAllWaves());
    }

    /// <summary>
    /// Checks for the debug key press to restart the wave sequence and the End Scene key press.
    /// </summary>
    void Update()
    {
        // Check for Numpad 3 key press to restart the waves
        if (Input.GetKeyDown(KeyCode.Keypad3))
        {
            RestartWaveSequence();
        }

        // Check for Space key press to end the scene after completion
        if (isLevelComplete && Input.GetKeyDown(KeyCode.Space))
        {
            EndScene();
        }
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
        isLevelComplete = false;

        // 4. Disable completion text (as requested)
        if (completionUIObject != null)
        {
            completionUIObject.SetActive(false);
        }

        // 5. Start the sequence again
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
            // ... (Wave spawning logic remains the same) ...
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
        HandleAllWavesCompleted();
    }

    /// <summary>
    /// Executes the game logic when the final wave is cleared.
    /// </summary>
    private void HandleAllWavesCompleted()
    {
        Debug.Log("--- All Waves Complete! Press SPACE to continue. ---");
        isLevelComplete = true;
        spawnWavesCoroutine = null; // Clear the coroutine reference as it is finished

        // Show the completion text/UI object
        if (completionUIObject != null)
        {
            completionUIObject.SetActive(true);
        }
    }

    /// <summary>
    /// Loads the next scene in the sequence, or quits the application if no scene is defined.
    /// </summary>
    private void EndScene()
    {
        // Check if a next scene is specified
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            Debug.Log($"Ending scene: Loading {nextSceneName}...");
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            // If no scene name is set, quit the application (for standalone builds)
            Debug.Log("Ending game: nextSceneName is empty. Application quitting.");

#if UNITY_EDITOR
            // Stop playing in the Unity Editor
            UnityEditor.EditorApplication.isPlaying = false;
#else
                // Quit the application in a standalone build
                Application.Quit();
#endif
        }
    }

    // Optional: Add a public method to check if spawning is finished
    public bool IsSpawningFinished()
    {
        return waveIndex >= waves.Length && currentWaveInstance == null;
    }
}
