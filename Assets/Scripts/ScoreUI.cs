using UnityEngine;
using TMPro; // **PERUBAHAN UTAMA: Tambahkan namespace ini**

/// <summary>
/// Menerima pembaruan skor dari GameManager dan menampilkannya di UI menggunakan TextMeshPro.
/// </summary>
public class ScoreDisplay_TMP : MonoBehaviour
{
    [Tooltip("Komponen TextMeshPro tempat skor akan ditampilkan.")]
    [SerializeField] private TMP_Text scoreText; // **PERUBAHAN: Menggunakan TMP_Text**

    void Start()
    {
        // Pastikan komponen TMP_Text sudah terhubung di Inspector
        if (scoreText == null)
        {
            Debug.LogError("Score Text (TMP_Text) is not assigned in the Inspector!");
            return;
        }

        if (GameManager.Instance != null)
        {
            // 1. Langganan ke event OnScoreChanged.
            GameManager.Instance.OnScoreChanged += UpdateScoreUI;

            // 2. Tampilkan skor awal segera.
            UpdateScoreUI(GameManager.Instance.GetCurrentScore());
        }
        else
        {
            Debug.LogError("GameManager Instance is null! ScoreDisplay cannot subscribe to score events.");
        }
    }

    void OnDestroy()
    {
        // Berhenti melanggan saat objek dihancurkan untuk mencegah memory leak.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged -= UpdateScoreUI;
        }
    }

    /// <summary>
    /// Fungsi callback yang dipanggil oleh event OnScoreChanged.
    /// </summary>
    /// <param name="newScore">Nilai skor yang baru.</param>
    private void UpdateScoreUI(int newScore)
    {
        // PERUBAHAN: Metode ini sama, hanya saja sekarang mengubah properti dari TMP_Text
        if (scoreText != null)
        {
            // Format tampilan skor. Misalnya, menggunakan format pemisah ribuan (:N0)
            scoreText.text = $"{newScore:N0}";
        }
    }
}