using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Text;

/// <summary>
/// Menampilkan konsol debug di layar yang menangkap semua log Unity (Log, Warning, Error).
/// Konsol ini menggunakan GUI standar Unity, yang sangat efisien untuk tujuan debugging.
/// </summary>
public class InGameConsole : MonoBehaviour
{
    // --- Pengaturan ---
    [Header("Console Settings")]
    public KeyCode toggleKey = KeyCode.F1; // Tombol untuk menampilkan/menyembunyikan konsol
    public int maxLogMessages = 50; // Jumlah maksimum pesan yang disimpan
    public bool visible = false; // Status awal konsol
    public float padding = 10f; // Jarak tepi konsol dari layar

    // --- State ---
    private List<LogEntry> logEntries = new List<LogEntry>();
    private Vector2 scrollPosition;

    // Struktur untuk menyimpan setiap pesan log
    private struct LogEntry
    {
        public string message;
        public LogType type;
        public Color color;
    }

    private void Awake()
    {
        // Pastikan objek ini tidak hancur saat scene berpindah
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // Mulai mendengarkan semua pesan log dari Unity
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        // Hentikan mendengarkan saat objek dinonaktifkan atau dihancurkan
        Application.logMessageReceived -= HandleLog;
    }

    private void Update()
    {
        // Toggle konsol menggunakan tombol yang ditentukan
        if (Input.GetKeyDown(toggleKey))
        {
            visible = !visible;
        }
    }

    /// <summary>
    /// Handler yang dipanggil setiap kali pesan log dicetak.
    /// </summary>
    /// <param name="logString">Isi pesan log.</param>
    /// <param name="stackTrace">Stack trace (diabaikan untuk kesederhanaan).</param>
    /// <param name="type">Jenis pesan log (Log, Error, Warning, dll.).</param>
    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        Color logColor;
        // Tentukan warna berdasarkan tipe log
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                logColor = Color.red;
                break;
            case LogType.Warning:
                logColor = Color.yellow;
                break;
            default:
                logColor = Color.white;
                break;
        }

        // Simpan pesan log
        logEntries.Add(new LogEntry
        {
            message = logString,
            type = type,
            color = logColor
        });

        // Hapus pesan terlama jika batas maksimum terlampaui
        if (logEntries.Count > maxLogMessages)
        {
            logEntries.RemoveAt(0);
        }

        // Atur posisi scroll ke bawah secara otomatis
        scrollPosition.y = float.MaxValue;
    }

    // --- Unity GUI (OnGUI) untuk Tampilan Konsol ---
    private void OnGUI()
    {
        if (!visible) return;

        // Tentukan area konsol (setengah lebar layar, 80% tinggi layar)
        float width = Screen.width / 2f;
        float height = Screen.height * 0.8f;
        Rect consoleRect = new Rect(padding, padding, width - (2 * padding), height);

        // Buat style untuk background transparan
        GUIStyle backgroundStyle = new GUIStyle(GUI.skin.box);
        backgroundStyle.normal.background = MakeTex((int)width, (int)height, new Color(0f, 0f, 0f, 0.7f)); // Background semi-transparan hitam

        // Gambar background
        GUI.Box(consoleRect, "", backgroundStyle);

        // Area dalam untuk scrolling
        Rect contentRect = new Rect(0, 0, consoleRect.width - 20, logEntries.Count * 20f);

        // Mulai area scroll
        scrollPosition = GUI.BeginScrollView(
            new Rect(consoleRect.x + padding, consoleRect.y + padding, consoleRect.width - (2 * padding), consoleRect.height - (2 * padding) - 30),
            scrollPosition,
            contentRect
        );

        // Tampilkan pesan log
        float yPos = 0;
        GUIStyle logStyle = new GUIStyle(GUI.skin.label);
        logStyle.wordWrap = true;
        logStyle.clipping = TextClipping.Overflow;

        foreach (var entry in logEntries)
        {
            logStyle.normal.textColor = entry.color;
            GUI.Label(new Rect(0, yPos, contentRect.width, 20f), $"[{entry.type.ToString()}] {entry.message}", logStyle);
            yPos += 20; // Pindah ke baris berikutnya
        }

        GUI.EndScrollView();

        // Tombol untuk menghapus log
        if (GUI.Button(new Rect(consoleRect.x, consoleRect.y + consoleRect.height - 30, 80, 25), "Clear"))
        {
            logEntries.Clear();
        }

        // Tampilkan instruksi
        GUIStyle infoStyle = new GUIStyle(GUI.skin.label);
        infoStyle.normal.textColor = Color.gray;
        GUI.Label(new Rect(consoleRect.x + 90, consoleRect.y + consoleRect.height - 30, 300, 25), $"Press '{toggleKey.ToString()}' to hide", infoStyle);
    }

    // Fungsi utilitas untuk membuat tekstur solid (untuk background)
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
