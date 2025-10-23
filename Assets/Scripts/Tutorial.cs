using UnityEngine;
using System.Collections;
using TMPro; // Untuk TextMeshProUGUI
using UnityEngine.UI; // Untuk Image
using System.Linq; // Untuk Linq methods

/// <summary>
/// Serializable class untuk menyimpan data setiap langkah dialog tutorial.
/// </summary>
[System.Serializable]
public class TutorialStep
{
    [Header("Dialogue Content")]
    [Tooltip("The 0-based index of the speaker's profile in the All Dialogue Profiles array.")]
    public int speakerProfileIndex = -1; // NEW: Menggantikan speakerProfile SO

    [Tooltip("The dialogue text to display.")]
    [TextArea(3, 5)]
    public string dialogueText;
    [Tooltip("The SoundManager key for the voiceover clip.")]
    public string voiceoverKey;

    [Header("Step Control")]
    [Tooltip("The minimum time (in seconds) to wait before automatically advancing to the next step, regardless of input.")]
    public float minWaitTime = 1.0f;
    [Tooltip("If true, the step requires player input (mouse move, fire) to advance.")]
    public bool requiresPlayerAction = false;

    // NEW: Field untuk menentukan aksi yang diperlukan (digunakan oleh CheckActionForCurrentStep)
    public PlayerActionType requiredAction = PlayerActionType.None;

    // NEW TOGGLES: Kontrol Pergerakan/Senjata per langkah
    [Header("Input Control Override (Per Step)")]
    [Tooltip("If true, enables PlayerController.HandleMovementInput() during this step.")]
    public bool enableMovementOnStep = false; // NEW
    [Tooltip("If true, enables PlayerController.HandleWeaponInput() during this step.")]
    public bool enableWeaponsOnStep = false; // NEW

    [Tooltip("If true, enables the Gun script (primary fire logic) during this step.")]
    public bool enableGunOnStep = false; // NEW: TOGGLE GUN BARU

    [Tooltip("If true, enables the PayloadManager script during this step.")]
    public bool enablePayloadOnStep = false; // NEW: TOGGLE PAYLOAD BARU

    [Header("Enemy Spawning (Optional)")]
    [Tooltip("The enemy wave prefab to spawn for this specific step (e.g., for shooting practice).")]
    public GameObject wavePrefab; // NEW
}

// NEW: Enum untuk aksi yang diperlukan
public enum PlayerActionType
{
    None,
    Movement,
    PrimaryFire,
    PayloadFire,
    // NEW: Action yang memerlukan pembersihan musuh
    ClearEnemiesPrimary, // NEW
    ClearEnemiesPayload // NEW
}


/// <summary>
/// Singleton manager to control the flow, input, and UI display during the tutorial stage.
/// </summary>
public class Tutorial : MonoBehaviour
{
    public static Tutorial Instance { get; private set; }

    [Header("UI References")]
    public GameObject dialogueBoxUI;
    public TextMeshProUGUI profileNameText;
    public TextMeshProUGUI factionText;
    public Image characterPortrait;
    public TextMeshProUGUI dialogueText;

    // MODIFIKASI: Tambahkan referensi untuk UI Gun Box
    [Tooltip("The parent GameObject for the UI element that shows gun/weapon information.")]
    public GameObject gunBoxUI; // UI Gun Box

    // NEW ARRAY: Array SO Dialog terpusat
    [Header("Dialogue Profiles")]
    [Tooltip("Array containing ALL available character profiles for the entire tutorial.")]
    public DialogueProfileSO[] allDialogueProfiles;

    [Header("Tutorial Stages")]
    public TutorialStep[] preparationSteps;
    public TutorialStep[] movementSteps;
    public TutorialStep[] shootingSteps; // Harus menggunakan ClearEnemiesPrimary
    public TutorialStep[] payloadSteps; // Harus menggunakan ClearEnemiesPayload
    public TutorialStep[] finalSteps;

    [Header("Player Dependencies")]
    [Tooltip("Reference to the PlayerController script.")]
    public PlayerController playerController;
    [Tooltip("Reference to the Gun component on the Player's aircraft.")]
    public Gun playerGun;
    [Tooltip("Reference to the PayloadManager component on the Player's aircraft.")]
    public PayloadManager payloadManager;

    // NEW: Transform tempat musuh akan di-spawn
    [Header("Enemy Spawner")]
    [Tooltip("The transform where enemy wave prefabs will be instantiated.")]
    public Transform enemySpawnerTransform; // NEW

    private Coroutine tutorialFlowCoroutine;
    private TutorialStep activeStep = null;
    private bool isWaitingForAction = false;
    private GameObject currentWaveInstance = null; // NEW: Melacak wave yang sedang aktif

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        // Matikan UI dialog secara default
        if (dialogueBoxUI != null) dialogueBoxUI.SetActive(false);
        // MATIKAN UI Gun Box secara default
        if (gunBoxUI != null) gunBoxUI.SetActive(false);
    }

    void Start()
    {
        // Cari dependensi Player jika belum terpasang di Inspector (fallback)
        FindPlayerDependencies();

        // Mulai tutorial
        if (tutorialFlowCoroutine != null) StopCoroutine(tutorialFlowCoroutine);

        // PENTING: Panggil InitializePlayerState di sini dengan semua false
        // Ini memastikan kontrol dimatikan SEBELUM StartCoroutine dipanggil.
        InitializePlayerState(false, false, false, false);

        tutorialFlowCoroutine = StartCoroutine(TutorialFlow());
    }

    /// <summary>
    /// Mencari PlayerController, Gun, dan PayloadManager di GameObject dengan tag "Player".
    /// </summary>
    private void FindPlayerDependencies()
    {
        if (playerController != null && playerGun != null && payloadManager != null) return;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            if (playerController == null) playerController = playerObj.GetComponent<PlayerController>();
            if (playerGun == null) playerGun = playerObj.GetComponentInChildren<Gun>();
            if (payloadManager == null) payloadManager = playerObj.GetComponentInChildren<PayloadManager>();
        }

        // Final check
        if (playerController == null || playerGun == null || payloadManager == null)
        {
            Debug.LogError("Tutorial dependencies (PlayerController, Gun, or PayloadManager) are missing on the 'Player' tagged object or in the Inspector.");
        }
    }


    /// <summary>
    /// Logic utama yang mengontrol setiap tahapan tutorial secara berurutan.
    /// </summary>
    private IEnumerator TutorialFlow()
    {
        Debug.Log("--- Tutorial Started ---");

        // --- Persiapan Awal ---
        // State sudah diatur ke false di Start() sebelum coroutine dimulai.
        yield return StartCoroutine(RunDialogueSteps(preparationSteps, PlayerActionType.None));

        // --- Tahapan 1: Gerak Mouse ---
        Debug.Log("--- Stage 1: Movement ---");
        InitializePlayerState(false, false, false, false);
        yield return StartCoroutine(RunDialogueSteps(movementSteps, PlayerActionType.Movement));

        // --- Tahapan 2: Menembak Senjata Utama & Clear Musuh ---
        Debug.Log("--- Stage 2: Primary Fire & Clear Enemies ---");
        // State dasar tahap ini: Movement ON, Weapon ON, Gun ON, Payload OFF
        InitializePlayerState(true, true, true, false);
        yield return StartCoroutine(RunDialogueSteps(shootingSteps, PlayerActionType.ClearEnemiesPrimary));

        // --- Tahapan 3: Payload & Clear Musuh ---
        Debug.Log("--- Stage 3: Payload & Clear Enemies ---");
        // State dasar tahap ini: Movement ON, Weapon ON, Gun ON, Payload ON
        InitializePlayerState(true, true, true, true);
        yield return StartCoroutine(RunDialogueSteps(payloadSteps, PlayerActionType.ClearEnemiesPayload));

        // --- Tahapan Terakhir: Dialog Penutup ---
        Debug.Log("--- Stage 4: Final Dialogue ---");
        InitializePlayerState(false, false, false, false); // Matikan semua kontrol
        yield return StartCoroutine(RunDialogueSteps(finalSteps, PlayerActionType.None));

        // --- Tutorial Selesai ---
        FinalizeTutorial();
        Debug.Log("--- Tutorial Finished ---");
    }

    /// <summary>
    /// Mengatur status PlayerController, Gun, PayloadManager, dan UI Gun Box.
    /// Ini adalah state dasar untuk seluruh segmen tutorial.
    /// </summary>
    private void InitializePlayerState(bool movement, bool weapons, bool gunEnabled, bool payloadEnabled)
    {
        if (playerController != null)
        {
            playerController.SetMovementEnabled(movement);
            playerController.SetWeaponsEnabled(weapons);
        }

        if (playerGun != null)
        {
            playerGun.SetFiringEnabled(gunEnabled);
        }

        if (payloadManager != null)
        {
            payloadManager.SetPayloadEnabled(payloadEnabled);
        }

        // PERUBAHAN KRITIS: Kontrol UI Gun Box berdasarkan status 'gunEnabled'
        if (gunBoxUI != null)
        {
            gunBoxUI.SetActive(gunEnabled);
        }
    }

    /// <summary>
    /// Memproses array langkah dialog satu per satu, menunggu input pemain jika diperlukan.
    /// </summary>
    private IEnumerator RunDialogueSteps(TutorialStep[] steps, PlayerActionType stageActionType)
    {
        foreach (var step in steps)
        {
            activeStep = step; // Set langkah yang sedang aktif

            // 1. **APPLY OVERRIDE TOGGLES**
            // Simpan status Gun untuk kontrol UI Gun Box
            bool enableGun = step.enableGunOnStep;

            if (playerController != null)
            {
                playerController.SetMovementEnabled(step.enableMovementOnStep);
                playerController.SetWeaponsEnabled(step.enableWeaponsOnStep);
            }

            // PENTING: Gunakan toggle GUN dan PAYLOAD BARU
            if (playerGun != null) playerGun.SetFiringEnabled(enableGun); // <-- MENGGUNAKAN enableGunOnStep
            if (payloadManager != null) payloadManager.SetPayloadEnabled(step.enablePayloadOnStep);

            // PERUBAHAN KRITIS: Kontrol UI Gun Box menggunakan toggle 'enableGunOnStep'
            if (gunBoxUI != null)
            {
                gunBoxUI.SetActive(enableGun);
            }
            // ----------------------------------------------------------------------


            // Tampilkan UI
            UpdateDialogueUI(step);

            // Putar Voiceover & Tunggu Min Time
            if (SoundManager.Instance != null && !string.IsNullOrEmpty(step.voiceoverKey))
            {
                SoundManager.Instance.PlayVoice(step.voiceoverKey);
                yield return new WaitForSeconds(step.minWaitTime);
            }
            else
            {
                yield return new WaitForSeconds(step.minWaitTime);
            }

            // 2. Jika langkah ini memerlukan aksi pemain atau spawning wave
            if (step.requiresPlayerAction && playerController != null)
            {
                // ** LOGIKA SPANWAVE **
                if (step.wavePrefab != null && enemySpawnerTransform != null)
                {
                    currentWaveInstance = Instantiate(step.wavePrefab, enemySpawnerTransform.position, enemySpawnerTransform.rotation);
                    Debug.Log($"[Tutorial] Spawned wave prefab: {step.wavePrefab.name}");
                }
                else if (step.wavePrefab != null)
                {
                    Debug.LogWarning("[Tutorial] wavePrefab is set but enemySpawnerTransform is missing! Cannot spawn wave.");
                }
                // ----------------------

                isWaitingForAction = true;

                Debug.Log($"[Tutorial] Waiting for player action: {stageActionType}.");

                // Tunggu hingga aksi yang sesuai terdeteksi
                yield return new WaitUntil(() => CheckActionForCurrentStep(stageActionType));

                isWaitingForAction = false;

                // 3. ** LOGIKA PEMBERSIHAN WAVE **
                if (stageActionType == PlayerActionType.ClearEnemiesPrimary || stageActionType == PlayerActionType.ClearEnemiesPayload)
                {
                    // Hancurkan wave yang sudah bersih
                    if (currentWaveInstance != null)
                    {
                        Destroy(currentWaveInstance);
                        currentWaveInstance = null;
                        Debug.Log("[Tutorial] Wave cleared and destroyed.");
                    }
                }

                // PENTING: Matikan semua kontrol input spesifik setelah aksi selesai
                playerController.SetMovementEnabled(false);
                playerController.SetWeaponsEnabled(false);
                playerGun.SetFiringEnabled(false);
                payloadManager.SetPayloadEnabled(false);

                // BARU: Matikan UI Gun Box juga
                if (gunBoxUI != null) gunBoxUI.SetActive(false);
            }
        }
        activeStep = null; // Kosongkan langkah aktif setelah segmen selesai
        UpdateDialogueUI(null);
    }

    /// <summary>
    /// Memeriksa input spesifik berdasarkan jenis aksi yang diminta oleh tahapan.
    /// </summary>
    private bool CheckActionForCurrentStep(PlayerActionType actionType)
    {
        if (playerController == null) return true;

        switch (actionType)
        {
            case PlayerActionType.Movement:
                return playerController.MovementDetected;
            case PlayerActionType.PrimaryFire:
                return playerController.PrimaryFireDetected;
            case PlayerActionType.PayloadFire:
                return playerController.PayloadFireDetected;
            case PlayerActionType.ClearEnemiesPrimary:
            case PlayerActionType.ClearEnemiesPayload:
                // Check jika wave ada DAN tidak memiliki child (semua musuh hancur)
                if (currentWaveInstance != null)
                {
                    return currentWaveInstance.transform.childCount == 0;
                }
                // Jika tidak ada wave yang di-spawn, anggap sudah selesai
                return true;
            default:
                return false;
        }
    }


    /// <summary>
    /// Memperbarui UI dialog berdasarkan data dari TutorialStep.
    /// </summary>
    private void UpdateDialogueUI(TutorialStep step)
    {
        if (dialogueBoxUI == null) return;

        // ** LOGIKA PENGAMBILAN PROFILE DARI ARRAY TERPUSAT **
        DialogueProfileSO profile = null;
        if (step != null && step.speakerProfileIndex >= 0 && step.speakerProfileIndex < allDialogueProfiles.Length)
        {
            profile = allDialogueProfiles[step.speakerProfileIndex];
        }
        // ****************************************************

        if (step != null)
        {
            dialogueBoxUI.SetActive(true);

            // Set Text dan Image
            if (dialogueText != null) dialogueText.text = step.dialogueText;

            if (profile != null)
            {
                if (profileNameText != null) profileNameText.text = profile.profileName;
                if (factionText != null) factionText.text = profile.faction;
                if (characterPortrait != null)
                {
                    characterPortrait.sprite = profile.characterPortrait;
                    characterPortrait.enabled = (profile.characterPortrait != null);
                }
            }
            else
            {
                // Clear UI jika profile null atau indeks invalid
                if (profileNameText != null) profileNameText.text = "SYSTEM";
                if (factionText != null) factionText.text = "NONE";
                if (characterPortrait != null) characterPortrait.enabled = false;
            }
        }
        else
        {
            // Sembunyikan UI
            dialogueBoxUI.SetActive(false);
        }
    }

    /// <summary>
    /// Logika setelah tutorial selesai (misalnya memulai wave musuh atau memuat scene baru).
    /// </summary>
    private void FinalizeTutorial()
    {
        // Aktifkan kembali semua kontrol pemain
        InitializePlayerState(true, true, true, true);

        if (Input.GetKeyDown(KeyCode.Keypad4))
        {
            Application.Quit();
        }

        // Contoh: Muat scene berikutnya atau aktifkan WaveSpawner
        Debug.Log("Tutorial completed. Full control returned to player.");
    }
}
