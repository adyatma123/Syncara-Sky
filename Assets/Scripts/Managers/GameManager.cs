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

    [Tooltip("The UI Text or GameObject to display when all waves are complete.")]
    public string levelMusic;

    [Tooltip("The name of the scene to load when the player presses SPACE after completion. Leave empty to quit the application.")]
    public string nextSceneName = "MainMenu";

    private bool isLevelComplete = false;
    // -------------------------------------------------------------


    // --- PAUSE MENU PROPERTIES (NEW SECTION) ---
    [Header("Pause Management")]
    [Tooltip("The root GameObject for the Pause Menu UI.")]
    public GameObject pauseMenuUI;

    [Tooltip("The name of the scene to load when the player press hangar button")]
    public string HangarScene;

    [Tooltip("The name of the scene to load when the player press exit button")]
    public string ExitScene;

    private bool isPaused = false;
    // -------------------------------------------

    public MissionCompleteUI missionUI;


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
        // NEW: Ensure Pause UI is disabled at the start
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
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

        // NEW: Check for Pause/Resume input (Escape key)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        EnemyProps.OnEnemyDestroyed -= IncrementTotalEnemiesDestroyed;
        EnemyProps.OnEnemyDestroyedByPlayerScore -= IncrementEnemiesKilledByPlayer;
    }


    // --- PAUSE MENU FUNCTIONS (NEW SECTION) ---

    /// <summary>
    /// Jeda permainan, menghentikan waktu dan menampilkan menu jeda.
    /// </summary>
    public void PauseGame()
    {
        if (isLevelComplete) return; // Jangan jeda jika level sudah selesai

        isPaused = true;
        Time.timeScale = 0f; // Menghentikan semua game logic (termasuk fisika)

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
        }

        // Opsional: Play SFX atau ubah musik
        // SoundManager.Instance.PlaySFX("Pause"); 
    }

    /// <summary>
    /// Melanjutkan permainan dari jeda. Dipanggil oleh tombol "Resume" atau Escape.
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Mengembalikan kecepatan waktu normal

        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }

        // Opsional: Play SFX atau ubah musik
        // SoundManager.Instance.PlaySFX("Resume");
    }

    /// <summary>
    /// Membuka menu Opsi (sementara hanya logging).
    /// </summary>
    public void OpenOptions()
    {
        Debug.Log("Options button pressed. Opening Options UI...");
        // Di sini Anda akan menambahkan logika untuk menampilkan UI Opsi dan menyembunyikan Pause Menu.
    }

    /// <summary>
    /// Memuat ulang scene saat ini.
    /// </summary>
    public void ResetScene()
    {
        ResumeGame(); // Pastikan TimeScale diatur ulang
        Debug.Log("Resetting current scene...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Kembali ke scene Hangar (SelectionTesting).
    /// </summary>
    public void LoadHangar()
    {
        ResumeGame(); // Pastikan TimeScale diatur ulang
        string hangarSceneName = "SelectionTesting"; // Sesuaikan dengan nama scene Hangar Anda
        Debug.Log($"Loading Hangar scene: {hangarSceneName}...");
        SceneManager.LoadScene(hangarSceneName);
    }

    /// <summary>
    /// Keluar dari aplikasi.
    /// </summary>
    public void ExitToDesktop()
    {
        Debug.Log("Exiting application...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }


    // --- LEVEL FLOW MANAGEMENT (MOVED FROM WAVESPAWNER) ---

    /// <summary>
    /// Called by the WaveSpawner when the final wave is cleared.
    /// </summary>
    public void NotifyAllWavesCompleted()
    {
        Debug.Log("--- All Waves Complete! (GameManager notified) ---");
        isLevelComplete = true;

        // Pastikan game tidak dalam kondisi jeda saat menyelesaikan level
        if (isPaused) ResumeGame();

        // Music must be played after ResumeGame if it was paused
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayMusic("Mission Complete");
        }

        // This calls the logic we just wrote above
        if (missionUI != null)
        {
            missionUI.ShowMissionComplete();
        }
        else if (completionUIObject != null)
        {
            // Fallback for your old UI object if missionUI isn't assigned
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

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayMusic(levelMusic);
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