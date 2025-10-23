using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections; // PENTING: Tambahkan ini untuk Coroutine

// Asumsi: DialogueProfileSO.cs sudah tersedia di project
// public enum CharacterDisposition { Friendly, Enemy, Neutral }

/// <summary>
/// Defines the conditions for triggering a single story event (voice line).
/// This structure appears in the Inspector array.
/// </summary>
[System.Serializable]
public class StoryCheckpoint
{
    // NEW: Tambahkan kondisi pemicu baru
    public enum TriggerType { WaveIndex, TotalEnemiesDestroyed, PreviousCheckpointTriggered }

    [Header("Profile and Audio")]
    [Tooltip("The Scriptable Object defining the character's name and profile.")]
    public DialogueProfileSO speakerProfile; // NEW: Referensi ke SO

    [Tooltip("The unique ID (name/key) your SoundManager uses to play this voice clip.")]
    public string voiceClipID;

    // NEW: Jeda setelah event ini terpicu
    [Tooltip("Delay in seconds AFTER this event is played/triggered before the next one can be checked.")]
    public float delayAfterTrigger = 0f;

    [Tooltip("Check this if the voice clip should only play if all prior clips are finished.")]
    public bool waitForPreviousClip = true;

    [Header("Trigger Condition")]
    [Tooltip("What type of game progression should trigger this event.")]
    public TriggerType triggerType;

    [Tooltip("The required index (0-based) or count to trigger the event.")]
    public int requiredValue;

    // NEW: Untuk TriggerType.PreviousCheckpointTriggered
    [Tooltip("The index of the checkpoint that MUST have been triggered before this one can start. -1 means no specific previous requirement.")]
    public int requiredPreviousIndex = -1;

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

    // NEW EVENT: Dipicu ketika Story Checkpoint berhasil dipicu. 
    // Meneruskan index (0-based) dari checkpoint yang terpicu.
    public event Action<int> OnCheckpointTriggered;

    [Header("Story Checkpoints")]
    [Tooltip("Define multiple checkpoints to trigger voice lines based on game progression.")]
    public StoryCheckpoint[] storyCheckpoints;

    // State variables
    private int _currentKillCount = 0;
    private int _currentWaveIndex = 0;
    private bool _isClipPlaying = false;

    // NEW: Variabel untuk menyimpan Coroutine jeda
    private Coroutine _delayCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); 
        }
    }

    void Start()
    {
        // Asumsi WaveSpawner.Instance dan EnemyProps.OnEnemyDestroyed diatur di tempat lain
    }

    void OnDestroy()
    {
        // Unsubscribe logic should be here
    }

    // Dipanggil oleh SoundManager saat klip dialog selesai diputar
    public void NotifyVoiceClipFinished()
    {
        _isClipPlaying = false;
        // Pengecekan trigger otomatis terjadi setelah delay (jika ada) selesai
    }

    // Dipanggil oleh GameManager/EnemyProps
    public void IncrementEnemiesDestroyed()
    {
        _currentKillCount++;
        CheckTriggers();
    }

    // Dipanggil oleh WaveSpawner
    public void UpdateWaveIndex(int newIndex)
    {
        _currentWaveIndex = newIndex;
        CheckTriggers();
    }

    void Update()
    {
        // Pastikan pengecekan trigger dilakukan secara berkala jika tidak ada Coroutine delay
        if (!_isClipPlaying && _delayCoroutine == null)
        {
            CheckTriggers();
        }
    }


    // --- Private Condition Checking and Triggering ---

    private void CheckTriggers()
    {
        if (_isClipPlaying || _delayCoroutine != null) return; // Jangan cek jika sedang ada klip atau delay

        // Pengecekan pemicu harus iteratif
        for (int i = 0; i < storyCheckpoints.Length; i++)
        {
            var checkpoint = storyCheckpoints[i];

            if (checkpoint.hasTriggered) continue;

            if (checkpoint.waitForPreviousClip && _isClipPlaying) continue;

            // --- Pengecekan Kondisi ---
            bool conditionMet = false;

            switch (checkpoint.triggerType)
            {
                case StoryCheckpoint.TriggerType.WaveIndex:
                    conditionMet = _currentWaveIndex >= checkpoint.requiredValue;
                    break;
                case StoryCheckpoint.TriggerType.TotalEnemiesDestroyed:
                    conditionMet = _currentKillCount >= checkpoint.requiredValue;
                    break;
                case StoryCheckpoint.TriggerType.PreviousCheckpointTriggered:
                    conditionMet = CheckPreviousTrigger(i, checkpoint.requiredPreviousIndex);
                    break;
            }

            if (conditionMet)
            {
                TriggerEvent(checkpoint, i); // Kirim indeks untuk melanjutkan
                // Keluar dari loop setelah memicu event agar tidak memicu lebih dari satu per frame
                return;
            }
        }
    }

    // Logika pengecekan pemicu sebelumnya
    private bool CheckPreviousTrigger(int currentIndex, int requiredIndex)
    {
        // Kondisi 1: requiredPreviousIndex = -1 (Tidak ada persyaratan pemicu sebelumnya)
        // Checkpoint awal yang menggunakan tipe trigger ini akan langsung terpenuhi.
        if (requiredIndex == -1)
        {
            return true;
        }

        // Kondisi 2: requiredPreviousIndex valid dan sudah terpicu
        if (requiredIndex >= 0 && requiredIndex < storyCheckpoints.Length)
        {
            return storyCheckpoints[requiredIndex].hasTriggered;
        }

        // Kondisi 3: requiredPreviousIndex tidak valid
        Debug.LogError($"[Story Event Check] Checkpoint index {currentIndex} has an invalid requiredPreviousIndex: {requiredIndex}");
        return false;
    }


    private void TriggerEvent(StoryCheckpoint checkpoint, int index)
    {
        // Jika ada coroutine delay yang sedang berjalan (seharusnya sudah dicek di CheckTriggers, tapi untuk jaga-jaga)
        if (_delayCoroutine != null) StopCoroutine(_delayCoroutine);

        // Pemicu Event Publik
        OnCheckpointTriggered?.Invoke(index);

        _delayCoroutine = StartCoroutine(TriggerEventCoroutine(checkpoint, index));
    }

    // Coroutine untuk menangani logic TriggerEvent dan delay
    private IEnumerator TriggerEventCoroutine(StoryCheckpoint checkpoint, int index)
    {
        // 1. Lakukan Trigger
        checkpoint.hasTriggered = true;
        _isClipPlaying = true; // Tandai klip sedang diputar

        if (checkpoint.speakerProfile != null)
        {
            Debug.Log($"[Story Event] Triggered '{checkpoint.voiceClipID}' by {checkpoint.speakerProfile.profileName} (Index {index}).");
            // Logika untuk menampilkan nama/visual profil akan di tempat lain
        }

        // Putar Suara
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayVoice(checkpoint.voiceClipID);
        }
        else
        {
            Debug.LogError($"[Story Event] Cannot play voice clip '{checkpoint.voiceClipID}'. SoundManager.Instance is NULL.");
        }

        // 2. Tunggu hingga klip selesai diputar (Anda harus memanggil NotifyVoiceClipFinished() dari SoundManager)
        // Jika callback tidak dipanggil, _isClipPlaying akan tetap true hingga delay selesai.

        // 3. Tunggu delay yang ditentukan oleh checkpoint
        if (checkpoint.delayAfterTrigger > 0)
        {
            Debug.Log($"[Story Event] Waiting for {checkpoint.delayAfterTrigger} seconds before checking next event.");
            yield return new WaitForSeconds(checkpoint.delayAfterTrigger);
        }

        // Setelah delay selesai, reset Coroutine dan cek lagi
        _delayCoroutine = null;

        // Jika SoundManager belum memanggil NotifyVoiceClipFinished, tandai selesai sebagai fallback.
        if (_isClipPlaying) _isClipPlaying = false;

        // 4. Lakukan pengecekan trigger lagi untuk memicu event berikutnya (jika ada)
        CheckTriggers();
    }
}
