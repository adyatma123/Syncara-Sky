using UnityEngine;
using TMPro;
using System;

public class MissionCompleteUI : MonoBehaviour
{
    [Header("UI Text References")]
    [Tooltip("Text for: Playtime : XX:XX:XX")]
    public TextMeshProUGUI playtimeText;

    [Tooltip("Text for: Enemy Killed : [Killed]/[Total]")]
    public TextMeshProUGUI statsText;

    [Tooltip("Text for: TotalScore")]
    public TextMeshProUGUI scoreText;

    /// <summary>
    /// Called by GameManager when the mission is finished.
    /// </summary>
    public void ShowMissionComplete()
    {
        // 1. Get Data from Managers
        float sceneSeconds = TimeManager.Instance != null ? TimeManager.Instance.GetSceneElapsedTime() : 0f;
        int kills = GameManager.Instance.GetEnemiesKilledByPlayer();
        int totalEnemies = WaveSpawner.Instance != null ? WaveSpawner.Instance.GetTotalEnemyCount() : 0;
        int score = GameManager.Instance.GetCurrentScore();

        // 2. Format Playtime (Left Justify Label, Right Justify Value)
        // Using TMP tags: <align="left">Label</align><line-height=0><align="right">Value</align>
        string formattedTime = FormatTime(sceneSeconds);
        playtimeText.text = $"Playtime :" +
                            $"<pos=82%>{formattedTime}</align>";

        // 3. Format Stats (Left Label, Tabbed Kills, Right Score)
        // We use the <pos> tag for a "Tab Stop" effect
        statsText.text = $"Enemy Killed :" +
                         $"<pos=50%>{kills}/{totalEnemies}</pos>";

        scoreText.text = $"{score}";

        // 4. Activate the UI panel
        this.gameObject.SetActive(true);
    }

    private string FormatTime(float seconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(seconds);
        return string.Format("{0:D2}:{1:D2}:{2:D2}",
            (int)time.TotalHours,
            time.Minutes,
            time.Seconds);
    }
}