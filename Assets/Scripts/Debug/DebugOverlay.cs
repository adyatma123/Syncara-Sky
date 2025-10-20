using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Text;
using UnityEngine.SceneManagement;
using System.Linq;

/// <summary>
/// Tracks the current mouse position in screen coordinates and manages a toggleable debug overlay 
/// that displays key game state variables.
/// </summary>
public class MousePos : MonoBehaviour
{
    // EXISTING: Mouse position tracking
    [Header("Mouse Tracking")]
    [Tooltip("The last recorded screen position of the mouse.")]
    public Vector3 screenPosition;

    // NEW: UI and Control fields for the Debug Overlay
    [Header("Debug Overlay")]
    [Tooltip("The TextMeshProUGUI component that will display the debug information.")]
    public TextMeshProUGUI debugText;
    [Tooltip("Key to toggle the visibility of the debug text (default is backquote/tilde).")]
    public KeyCode toggleKey = KeyCode.BackQuote;

    // NEW: Manager References
    private AircraftController playerAircraft;
    private WaveSpawner waveSpawner;
    private GameManager gameManager;
    private BackgroundManager backgroundManager;
    private StoryEventManager storyEventManager;
    private SoundManager soundManager;
    private PayloadManager payloadManager;

    // NEW: Internal State
    private float startTime;
    private bool isVisible = false;

    void Start()
    {
        // NEW: Debug overlay initialization
        // 1. Find all necessary manager references (assumes singletons/FindObjectOfType pattern)
        // Note: As Singletons are persistent, they should be safe to find in Start().
        gameManager = GameManager.Instance;
        soundManager = SoundManager.Instance;
        waveSpawner = WaveSpawner.Instance;
        storyEventManager = StoryEventManager.Instance;
        backgroundManager = FindObjectOfType<BackgroundManager>();

        // 2. UI Validation
        if (debugText == null)
        {
            Debug.LogError("MousePos component (acting as Debug Overlay) requires a TextMeshProUGUI component assigned to debugText!");
        }

        // 3. Initial state
        if (debugText != null)
        {
            debugText.enabled = isVisible;
        }
        startTime = Time.time;
    }

    void Update()
    {
        // 1. DYNAMIC PLAYER AIRCRAFT FINDER (Hanya mencari jika belum ditemukan)
        if (playerAircraft == null)
        {
            FindPlayerAircraft();
        }

        // EXISTING: Mouse tracking
        screenPosition = Input.mousePosition;

        // NEW: Debug Overlay Update/Toggle Logic
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;
            if (debugText != null)
            {
                debugText.enabled = isVisible;
            }
        }

        if (isVisible && debugText != null)
        {
            debugText.text = BuildDebugString();
        }
    }

    /// <summary>
    /// Mencari objek pesawat pemain dan komponennya.
    /// </summary>
    private void FindPlayerAircraft()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerAircraft = playerObj.GetComponent<AircraftController>();

            if (playerAircraft != null)
            {
                // Setelah AircraftController ditemukan, ambil PayloadManager
                payloadManager = playerAircraft.payloadManager;
                Debug.Log("[Debug Overlay] Successfully found and tracked spawned player aircraft and PayloadManager.");
                SoundManager.Instance.PlaySFX("Click");
            }
            else
            {
                Debug.LogWarning("[Debug Overlay] Player object found, but missing AircraftController. Retrying next frame.");
            }
        }
    }

    /// <summary>
    /// Compiles all current game state information into a formatted string.
    /// </summary>
    private string BuildDebugString()
    {
        var sb = new StringBuilder();

        // --- GAME MANAGER & GLOBAL STATE ---
        sb.AppendLine("<b><color=#00FF00>--- GLOBAL STATE ---</color></b>");
        // Gunakan operator null-conditional (?.) agar kode tidak crash jika manager null
        sb.AppendLine($"Current Score: {gameManager?.GetCurrentScore() ?? 0}");
        sb.AppendLine($"Level Name: {SceneManager.GetActiveScene().name}");
        sb.AppendLine($"Total Score: {gameManager?.GetCurrentScore() ?? 0}");
        sb.AppendLine($"Enemy Killed: {gameManager?.GetTotalEnemiesDestroyed() ?? 0}");

        // Current Wave (Logika WaveSpawner tetap sama)
        if (waveSpawner != null && waveSpawner.waves != null && waveSpawner.waves.Length > 0)
        {
            string waveName = (waveSpawner.waveIndex < waveSpawner.waves.Length && waveSpawner.waves[waveSpawner.waveIndex].waveContainerPrefab != null) ?
                              waveSpawner.waves[waveSpawner.waveIndex].waveContainerPrefab.name :
                              "COMPLETED";
            int waveNumber = waveSpawner.waveIndex + 1;
            sb.AppendLine($"Current Wave: {waveName} ({waveNumber}/{waveSpawner.waves.Length})");
        }
        else
        {
            sb.AppendLine("Current Wave: N/A");
        }

        // Current Background Prefab
        sb.AppendLine($"Current background prefab: {backgroundManager?.backgroundPrefab?.name ?? "N/A"}");

        // Story Checkpoints
        sb.AppendLine("Story checkpoints:");
        if (storyEventManager != null && storyEventManager.storyCheckpoints != null)
        {
            foreach (var cp in storyEventManager.storyCheckpoints)
            {
                string status = cp.hasTriggered ? "<color=red>TRIGGERED</color>" : "Pending";
                sb.AppendLine($"  - {cp.voiceClipID}: {status} ({cp.triggerType}@{cp.requiredValue})");
            }
        }

        // Mouse Position
        sb.AppendLine($"Mouse position: {screenPosition.ToString()}");

        sb.AppendLine("---");

        // --- PLAYER AIRCRAFT ---
        sb.AppendLine("<b><color=#00FFFF>--- PLAYER AIRCRAFT ---</color></b>");
        if (playerAircraft != null)
        {
            Gun gun = playerAircraft.controlledGun;

            // FIX: Get Aircraft Name from the VehicleName property (which checks the SO first)
            sb.AppendLine($"Aircraft Name: {playerAircraft.VehicleName}");

            // Health
            sb.AppendLine($"Health: {playerAircraft.currentHealth}/{playerAircraft.maxHealth}");

            // Gun
            sb.AppendLine($"Gun Name: {gun?.guns?.name ?? "N/A"}");
            sb.AppendLine($"Gun Heat: {gun?.currentHeat.ToString("F1") ?? "0"}/{gun?.maxHeat.ToString("F1") ?? "0"}");

            // Payload
            sb.AppendLine($"Total payload (Types): {payloadManager?.GetTotalUniquePayloads() ?? 0}");
            sb.AppendLine("Payload names :");

            if (payloadManager != null)
            {
                List<string> payloadNames = payloadManager.GetPayloadNames();
                for (int i = 0; i < payloadNames.Count; i++)
                {
                    string status = (i == payloadManager.CurrentPayloadIndex) ? "<color=yellow>[*]</color> " : "";
                    sb.AppendLine($"  {status}- {payloadNames[i]}");
                }
                sb.AppendLine($"Current payload ammo left: {payloadManager.GetCurrentPayloadAmmoLeft()}");
            }
            else
            {
                sb.AppendLine("  - PayloadManager N/A");
                sb.AppendLine("Current payload ammo left: N/A");
            }

            // Aimbot
            sb.AppendLine($"Aimbot: {playerAircraft.IsAimbotActive}");

        }
        else
        {
            sb.AppendLine("Player Aircraft: <color=red>NOT FOUND (Searching...)</color>");
        }

        sb.AppendLine("---");

        // --- AUDIO STATE ---
        sb.AppendLine("<b><color=#FFFF00>--- AUDIO STATE ---</color></b>");
        sb.AppendLine($"Current Music Name: {soundManager?.CurrentMusicName ?? "N/A"}");
        sb.AppendLine($"Current Voice Name: {soundManager?.CurrentVoiceName ?? "N/A"}");

        sb.AppendLine("---");

        // --- TIME STATE ---
        sb.AppendLine("<b><color=#FF00FF>--- TIME ---</color></b>");
        sb.AppendLine($"Time elapsed: {Time.time - startTime:F2}s");

        return sb.ToString();
    }
}
