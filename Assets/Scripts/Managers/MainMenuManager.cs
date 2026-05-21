using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles Main Menu navigation and UI.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Management")]
    [Tooltip("Scene loaded when pressing Start.")]
    [SerializeField] private string startSceneName;

    [Tooltip("Fallback scene for Continue button.")]
    [SerializeField] private string continueSceneName;

    [Header("UI References")]
    [Tooltip("Main menu root UI.")]
    [SerializeField] private GameObject mainMenuUI;

    [Tooltip("Settings menu root UI.")]
    [SerializeField] private GameObject settingsMenuUI;

    [Header("Audio")]
    [Tooltip("Music key from SoundManager.")]
    [SerializeField] private string mainMenuMusic = "Main Menu";

    private const string SAVE_SCENE_KEY = "LastScene";

    private void Start()
    {
        // Ensure correct UI state
        if (mainMenuUI != null)
            mainMenuUI.SetActive(true);

        if (settingsMenuUI != null)
            settingsMenuUI.SetActive(false);

        // Play menu music
        if (SoundManager.Instance != null && !string.IsNullOrEmpty(mainMenuMusic))
        {
            SoundManager.Instance.PlayMusic(mainMenuMusic);
        }
    }

    /// <summary>
    /// Start a new game.
    /// </summary>
    public void StartGame()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopAllAudio();
        }

        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(startSceneName))
        {
            // Save current target scene for Continue
            PlayerPrefs.SetString(SAVE_SCENE_KEY, startSceneName);
            PlayerPrefs.Save();

            SceneManager.LoadScene(startSceneName);
        }
        else
        {
            Debug.LogWarning("Start Scene Name is empty.");
        }
    }

    /// <summary>
    /// Continue previous session.
    /// Placeholder for future save system.
    /// </summary>
    public void ContinueGame()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopAllAudio();
        }

        Time.timeScale = 1f;

        string savedScene = PlayerPrefs.GetString(SAVE_SCENE_KEY, continueSceneName);

        if (!string.IsNullOrEmpty(savedScene))
        {
            SceneManager.LoadScene(savedScene);
        }
        else
        {
            Debug.LogWarning("No saved scene found.");
        }
    }

    /// <summary>
    /// Open settings menu.
    /// </summary>
    public void OpenSettings()
    {
        if (mainMenuUI != null)
            mainMenuUI.SetActive(false);

        if (settingsMenuUI != null)
            settingsMenuUI.SetActive(true);
    }

    /// <summary>
    /// Return to main menu UI.
    /// </summary>
    public void CloseSettings()
    {
        if (settingsMenuUI != null)
            settingsMenuUI.SetActive(false);

        if (mainMenuUI != null)
            mainMenuUI.SetActive(true);
    }

    /// <summary>
    /// Exit the application.
    /// </summary>
    public void ExitGame()
    {
        Debug.Log("Exiting Game...");

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopAllAudio();
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}