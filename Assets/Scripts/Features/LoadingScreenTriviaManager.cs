using UnityEngine;
using TMPro; // Pastikan menggunakan TextMeshPro

/// <summary>
/// Mengelola dan menampilkan Trivia secara acak di layar loading,
/// serta menampilkan informasi Stage saat ini.
/// </summary>
public class LoadingScreenTriviaManager : MonoBehaviour
{
    [Header("Stage Info UI References")]
    [Tooltip("Komponen TMP_Text untuk menampilkan Nama Stage.")]
    [SerializeField] private TMP_Text stageNameText;

    [Tooltip("Komponen TMP_Text untuk menampilkan Lokasi.")]
    [SerializeField] private TMP_Text locationText;

    [Tooltip("Komponen TMP_Text untuk menampilkan Tanggal.")]
    [SerializeField] private TMP_Text dateText;


    [Header("Trivia UI References (TextMeshPro)")]
    [Tooltip("Komponen TMP_Text untuk menampilkan Judul Trivia.")]
    [SerializeField] private TMP_Text triviaNameText;

    [Tooltip("Komponen TMP_Text untuk menampilkan Konten Trivia.")]
    [SerializeField] private TMP_Text triviaContentText;


    [Header("Trivia Data")]
    [Tooltip("Array dari semua Scriptable Object TriviaData yang tersedia.")]
    [SerializeField] private TriviaData[] availableTrivias;

    [Header("Timing Settings")]
    [Tooltip("Waktu tunggu awal sebelum trivia pertama ditampilkan.")]
    [SerializeField] private float initialDelay = 1f;

    [Tooltip("Interval waktu (dalam detik) untuk mengganti trivia baru.")]
    [SerializeField] private float changeInterval = 15f; // **Interval ganti 15 detik**

    void Start()
    {
        // 1. Tampilkan data stage saat scene loading dimulai
        InitializeStageData();

        // 2. Panggil fungsi untuk menampilkan trivia secara berulang
        // Parameters: "Nama Fungsi", Waktu Tunggu Awal, Interval Pengulangan
        InvokeRepeating("DisplayRandomTrivia", initialDelay, changeInterval);
    }

    /// <summary>
    /// Mengambil StageData dari SceneLoader dan memperbarui teks UI.
    /// </summary>
    private void InitializeStageData()
    {
        // Pastikan SceneLoader ada dan memiliki data stage
        if (SceneLoader.Instance != null && SceneLoader.Instance.currentStageData != null)
        {
            StageData data = SceneLoader.Instance.currentStageData;

            if (stageNameText != null) stageNameText.text = data.stageName;
            if (locationText != null) locationText.text = data.location;
            if (dateText != null) dateText.text = data.date;
        }
        else
        {
            Debug.LogWarning("SceneLoader instance or StageData not found. Stage info UI will not be updated.");
        }
    }

    /// <summary>
    /// Memilih satu trivia secara acak dan memperbarui UI.
    /// Fungsi ini dipanggil secara berulang oleh InvokeRepeating.
    /// </summary>
    public void DisplayRandomTrivia()
    {
        // Pastikan ada trivia yang dimasukkan ke dalam array
        if (availableTrivias.Length == 0)
        {
            Debug.LogWarning("Tidak ada TriviaData yang ditemukan. UI Trivia akan kosong.");
            // Hentikan pengulangan jika tidak ada data
            CancelInvoke("DisplayRandomTrivia");

            if (triviaNameText != null) triviaNameText.text = "Error";
            if (triviaContentText != null) triviaContentText.text = "Mohon tambahkan data trivia.";
            return;
        }

        // 1. Pilih indeks acak
        int randomIndex = Random.Range(0, availableTrivias.Length);

        // 2. Ambil objek trivia yang terpilih
        TriviaData selectedTrivia = availableTrivias[randomIndex];

        // 3. Pastikan UI References tidak null sebelum mencoba memperbarui teks
        if (triviaNameText != null)
        {
            // Tampilkan Nama Trivia (jika ada)
            triviaNameText.text = string.IsNullOrEmpty(selectedTrivia.TriviaName)
                                ? "TRIVIA" // Default jika nama trivia kosong
                                : selectedTrivia.TriviaName.ToUpper();
        }

        if (triviaContentText != null)
        {
            // Tampilkan Konten Trivia
            triviaContentText.text = selectedTrivia.TriviaContent;
        }
    }
}