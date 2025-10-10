using UnityEngine;
using System;
using UnityEngine.SceneManagement; // Required for loading scenes

/// <summary>
/// Singleton class responsible for tracking global game state, including score, enemy statistics, 
/// and overall level flow (scene loading/completion).
/// </summary>
public class GameManager : MonoBehaviour
{
    // --- Singleton Implementation ---
    public static GameManager Instance { get; private set; }

    // --- Events for external systems to subscribe to ---
    public event Action<int> OnScoreChanged;
    public event Action<int> OnTotalEnemiesDestroyedChanged;

    [Header("Game State")]
    [Tooltip("The current player score.")]
    [SerializeField] private int currentScore = 0;

    [Tooltip("Total number of enemies destroyed by any means (player hit or out-of-bounds).")]
    [SerializeField] private int totalEnemiesDestroyed = 0;

    [Tooltip("Total number of enemies destroyed specifically by player projectiles (for accuracy/stats).")]
    [SerializeField] private int enemiesKilledByPlayer = 0;

    // --- LEVEL COMPLETION PROPERTIES (MOVED FROM WAVESPAWNER) ---
    [Header("Level Flow")]
    [Tooltip("The UI Text or GameObject to display when all waves are complete.")]
    public GameObject completionUIObject;

    [Tooltip("The name of the scene to load when the player presses SPACE after completion. Leave empty to quit the application.")]
    public string nextSceneName = "MainMenu";

    private bool isLevelComplete = false;
    // -------------------------------------------------------------


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Uncomment if the manager persists across scenes
        }

        // Ensure UI is disabled at the start of the scene
        if (completionUIObject != null)
        {
            completionUIObject.SetActive(false);
        }
    }

    void Start()
    {
        // Subscribe to the static enemy destruction event
        EnemyProps.OnEnemyDestroyed += IncrementTotalEnemiesDestroyed;
        EnemyProps.OnEnemyDestroyedByPlayerScore += IncrementEnemiesKilledByPlayer;

        // Initialize display values (optional, useful for UI refresh)
        OnScoreChanged?.Invoke(currentScore);
        OnTotalEnemiesDestroyedChanged?.Invoke(totalEnemiesDestroyed);
    }

    void Update()
    {
        // Check for Space key press to end the scene after completion
        if (isLevelComplete && Input.GetKeyDown(KeyCode.Space))
        {
            EndScene();
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        EnemyProps.OnEnemyDestroyed -= IncrementTotalEnemiesDestroyed;
        EnemyProps.OnEnemyDestroyedByPlayerScore -= IncrementEnemiesKilledByPlayer;
    }


    // --- LEVEL FLOW MANAGEMENT (MOVED FROM WAVESPAWNER) ---

    /// <summary>
    /// Called by the WaveSpawner when the final wave is cleared.
    /// </summary>
    public void NotifyAllWavesCompleted()
    {
        Debug.Log("--- All Waves Complete! (GameManager notified) ---");
        isLevelComplete = true;

        SoundManager.Instance.PlayMusic("Mission Complete");

        // Show the completion text/UI object
        if (completionUIObject != null)
        {
            completionUIObject.SetActive(true);
        }
    }

    /// <summary>
    /// Resets the level completion flag and hides the UI (called by WaveSpawner on Numpad 3 reset).
    /// </summary>
    public void ResetLevelState()
    {
        isLevelComplete = false;
        if (completionUIObject != null)
        {
            completionUIObject.SetActive(false);
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

    // --- GAME STATE MUTATORS ---

    public void AddScore(int amount)
    {
        currentScore += amount;
        OnScoreChanged?.Invoke(currentScore);
    }

    private void IncrementTotalEnemiesDestroyed()
    {
        totalEnemiesDestroyed++;
        OnTotalEnemiesDestroyedChanged?.Invoke(totalEnemiesDestroyed);

        // This is the call the StoryEventManager needs to check for kill count events!
        if (StoryEventManager.Instance != null)
        {
            StoryEventManager.Instance.IncrementEnemiesDestroyed();
        }
    }

    private void IncrementEnemiesKilledByPlayer(int scoreValue)
    {
        enemiesKilledByPlayer++;
        // The AddScore() call handles the score change event.
        AddScore(scoreValue);
    }


    // --- Public Accessors ---

    public int GetCurrentScore() => currentScore;
    public int GetTotalEnemiesDestroyed() => totalEnemiesDestroyed;
    public int GetEnemiesKilledByPlayer() => enemiesKilledByPlayer;
}
