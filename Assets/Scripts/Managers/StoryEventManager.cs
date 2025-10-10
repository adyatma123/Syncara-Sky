using UnityEngine;
using System;
using System.Collections.Generic;

// We assume a static helper class exists to call audio, matching your requirement:
// public static class SoundManager { public static SoundManager Instance; public void PlayVoice(string clipID); }

/// <summary>
/// Defines the conditions for triggering a single story event (voice line).
/// This structure appears in the Inspector array.
/// </summary>
[System.Serializable]
public class StoryCheckpoint
{
    public enum TriggerType { WaveIndex, TotalEnemiesDestroyed }

    [Header("Event Configuration")]
    [Tooltip("The unique ID (name/key) your SoundManager uses to play this voice clip.")]
    public string voiceClipID;

    [Tooltip("Check this if the voice clip should only play if all prior clips are finished.")]
    public bool waitForPreviousClip = true;

    [Header("Trigger Condition")]
    [Tooltip("What type of game progression should trigger this event.")]
    public TriggerType triggerType;

    [Tooltip("The required index (0-based) or count to trigger the event.")]
    public int requiredValue;

    [HideInInspector] public bool hasTriggered = false;
}

/// <summary>
/// A Singleton manager that monitors game state events (waves, kills) and triggers 
/// corresponding story voice lines defined in the Inspector array.
/// </summary>
public class StoryEventManager : MonoBehaviour
{
    // Singleton Instance
    public static StoryEventManager Instance { get; private set; }

    [Header("Story Checkpoints")]
    [Tooltip("Define multiple checkpoints to trigger voice lines based on game progression.")]
    public StoryCheckpoint[] storyCheckpoints;

    // Runtime state tracking (Assume these values are updated by a GameManager/WaveSpawner)
    private int _currentWaveIndex = 0;
    private int _totalEnemiesDestroyed = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Uncomment if this manager persists across scenes
        }
    }

    void Start()
    {
        // Subscribe to events from the WaveSpawner and Enemy system (assuming they are set up)
        if (WaveSpawner.Instance != null)
        {
            WaveSpawner.Instance.OnWaveCleared += CheckWaveTriggers;
            Debug.Log("[Story Event Manager] Subscribed to WaveSpawner events.");
        }
        else
        {
            Debug.LogError("[Story Event Manager] WaveSpawner Instance not found. Wave triggers are disabled.");
        }
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (WaveSpawner.Instance != null)
        {
            WaveSpawner.Instance.OnWaveCleared -= CheckWaveTriggers;
        }
    }

    // --- Public methods for external scripts (like GameManager or EnemyProps) to call ---

    /// <summary>
    /// Should be called by the WaveSpawner after a wave is cleared.
    /// </summary>
    public void UpdateWaveIndex(int newIndex)
    {
        _currentWaveIndex = newIndex;
        CheckWaveTriggers(newIndex);
    }

    /// <summary>
    /// Should be called whenever an enemy (by any means) is successfully destroyed.
    /// </summary>
    public void IncrementEnemiesDestroyed()
    {
        _totalEnemiesDestroyed++;
        Debug.Log($"[Story Event Manager] Total Kills updated to: {_totalEnemiesDestroyed}");
        CheckKillTriggers(_totalEnemiesDestroyed);
    }

    // --- Private Condition Checking ---

    private void CheckWaveTriggers(int waveIndex)
    {
        foreach (var checkpoint in storyCheckpoints)
        {
            if (checkpoint.hasTriggered) continue;

            if (checkpoint.triggerType == StoryCheckpoint.TriggerType.WaveIndex)
            {
                if (waveIndex == checkpoint.requiredValue)
                {
                    TriggerEvent(checkpoint);
                }
                else
                {
                    Debug.Log($"[Story Event Check] Wave Trigger skipped. Current Wave: {waveIndex} (Needed: {checkpoint.requiredValue})");
                }
            }
        }
    }

    private void CheckKillTriggers(int killCount)
    {
        foreach (var checkpoint in storyCheckpoints)
        {
            if (checkpoint.hasTriggered) continue;

            if (checkpoint.triggerType == StoryCheckpoint.TriggerType.TotalEnemiesDestroyed)
            {
                if (killCount >= checkpoint.requiredValue)
                {
                    TriggerEvent(checkpoint);
                }
                else
                {
                    // --- CRITICAL DEBUG LOG ---
                    Debug.Log($"[Story Event Check] Kill Trigger NOT met. Current Kills: {killCount} (Needed: {checkpoint.requiredValue}). Not yet triggering.");
                }
            }
        }
    }

    private void TriggerEvent(StoryCheckpoint checkpoint)
    {
        if (SoundManager.Instance != null)
        {
            Debug.Log($"[Story Event] Triggered '{checkpoint.voiceClipID}' at {checkpoint.triggerType}: {checkpoint.requiredValue}");
            SoundManager.Instance.PlayVoice(checkpoint.voiceClipID);
            checkpoint.hasTriggered = true;
        }
        else
        {
            // Changed to Error for high visibility if the audio system is missing
            Debug.LogError($"[Story Event] Cannot play voice clip '{checkpoint.voiceClipID}'. SoundManager.Instance is NULL. Check scene setup.");
        }
    }
}
