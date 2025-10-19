using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections; // Needed for the Coroutine if you want smooth progress, though Update works too

public class LoadingUIHandler : MonoBehaviour
{
    // Assign references in the Inspector of the Loading Scene
    [Header("UI References")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private Text loadingText;

    private AsyncOperation currentOperation;

    // --- SETUP ---
    void Start()
    {
        // Pastikan UI references sudah di-assign
        if (progressBar == null || loadingText == null)
        {
            Debug.LogError("Loading UI components (Slider/Text) are not assigned to LoadingUIHandler.");
            return;
        }

        // 1. Dapatkan referensi SceneLoader (Singleton yang persistent)
        if (SceneLoader.Instance != null)
        {
            // 2. Minta SceneLoader untuk memulai operasi loading, dan simpan hasilnya.
            currentOperation = SceneLoader.Instance.StartLoadingOperation();
            if (currentOperation == null)
            {
                // Ini terjadi jika tidak ada scene yang di-queue sebelum LoadScene dipanggil.
                loadingText.text = "Error: No scene queued for loading!";
            }
        }
        else
        {
            Debug.LogError("SceneLoader instance not found. Cannot start loading.");
            enabled = false;
        }
    }

    // --- UPDATE PROGRESS ---
    void Update()
    {
        // Hanya jalan jika operasi loading sudah dimulai
        if (currentOperation == null) return;

        // Progress goes from 0 to 0.9. We normalize it to 0-1.
        float progress = Mathf.Clamp01(currentOperation.progress / 0.9f);

        // Update UI: Progress Bar & Percentage Text
        progressBar.value = progress;
        loadingText.text = "Loading: " + (progress * 100f).ToString("F0") + "%";

        // Cek jika loading sudah selesai secara teknis (mencapai 0.9)
        if (currentOperation.progress >= 0.9f)
        {
            progressBar.value = 1f; // Force to 100%
            loadingText.text = "Ready. Press any key to continue...";

            // Tunggu input pengguna untuk aktivasi scene
            if (Input.anyKey)
            {
                // AKTIVASI SCENE TARGET
                currentOperation.allowSceneActivation = true;
            }
        }
    }
}
