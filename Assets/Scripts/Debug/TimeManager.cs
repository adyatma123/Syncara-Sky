using UnityEngine;
using UnityEngine.SceneManagement; // Diperlukan untuk mendengarkan event pergantian Scene

/// <summary>
/// Singleton yang persisten (DontDestroyOnLoad) untuk melacak waktu bermain
/// total (across scenes) dan waktu bermain scene saat ini.
/// </summary>
public class TimeManager : MonoBehaviour
{
    // Singleton Instance
    public static TimeManager Instance { get; private set; }

    [Header("Global Time Tracking")]
    [Tooltip("Waktu total (dalam detik) sejak permainan dimulai (persisten antar scene).")]
    public float totalElapsedTime = 0f;

    [Header("Scene Time Tracking")]
    [Tooltip("Waktu (dalam detik) ketika scene saat ini dimuat.")]
    private float sceneLoadTime;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Jika instance sudah ada, hapus objek ini
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Atur waktu awal scene saat pertama kali skrip dimulai.
        ResetSceneTimer();

        // PENTING: Subscribe ke event sceneLoaded agar timer direset setiap kali scene baru dimuat.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        // Penting: Unsubscribe saat objek dihancurkan untuk menghindari kebocoran memori.
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Dipanggil oleh SceneManager ketika scene baru selesai dimuat.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Panggil fungsi reset setiap kali scene baru dimuat.
        ResetSceneTimer();
    }

    void Update()
    {
        // Lacak waktu total permainan
        totalElapsedTime += Time.deltaTime;
    }

    /// <summary>
    /// Mengatur ulang scene timer ke waktu saat ini.
    /// </summary>
    public void ResetSceneTimer()
    {
        // Time.time adalah waktu total yang berlalu sejak game diluncurkan.
        // Dengan menyimpan Time.time saat scene dimuat, GetSceneElapsedTime dapat menghitung perbedaan.
        sceneLoadTime = Time.time;
        Debug.Log("[TimeManager] Scene timer reset for new scene.");
    }

    /// <summary>
    /// Mendapatkan waktu yang berlalu sejak scene saat ini dimuat.
    /// </summary>
    public float GetSceneElapsedTime()
    {
        // Mengembalikan perbedaan antara waktu game saat ini dan waktu scene dimuat.
        return Time.time - sceneLoadTime;
    }
}
