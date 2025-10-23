using UnityEngine;

/// <summary>
/// Skrip ini mengontrol rotasi lokal GameObject berdasarkan posisi horizontal (X)
/// dan kedalaman (Z) dari pemain dengan tag 'Player', memetakannya ke batas rotasi yang ditentukan.
/// </summary>
public class PlayerRotationController : MonoBehaviour
{
    [Header("Player Target")]
    [Tooltip("Referensi ke Transform pemain. Akan dicari secara otomatis dengan tag 'Player' jika kosong.")]
    public Transform playerTransform;

    [Header("Player Position Bounds (Input Range)")]
    [Tooltip("Posisi X maksimum yang dapat dicapai pemain. (Digunakan sebagai 100% Rotasi Y)")]
    public float playerMaxX = 10f;
    [Tooltip("Posisi X minimum yang dapat dicapai pemain. (Digunakan sebagai 0% Rotasi Y)")]
    public float playerMinX = -10f;

    // BARU: Batas untuk Posisi Z (kedalaman)
    [Tooltip("Posisi Z maksimum yang dapat dicapai pemain. (Digunakan sebagai 100% Rotasi X)")]
    public float playerMaxZ = 10f;
    [Tooltip("Posisi Z minimum yang dapat dicapai pemain. (Digunakan sebagai 0% Rotasi X)")]
    public float playerMinZ = -10f;

    // Rotasi Y (Vertikal) sebelumnya dikontrol oleh Posisi Y, sekarang diabaikan (atau bisa digunakan untuk Rotasi Z jika diperlukan).
    [Tooltip("Posisi Y (vertikal) pemain. Saat ini tidak digunakan untuk rotasi X atau Y, tetapi tersedia untuk referensi.")]
    public float playerMaxY = 5f;
    [Tooltip("Posisi Y (vertikal) pemain. Saat ini tidak digunakan untuk rotasi X atau Y, tetapi tersedia untuk referensi.")]
    public float playerMinY = -5f;


    [Header("Rotation Limits (Output Range)")]
    [Tooltip("Rotasi X maksimum saat pemain mencapai PlayerMaxZ (nilai positif) dan PlayerMinZ (nilai negatif).")]
    public float maxRotationX = 30f; // Rotasi sumbu X (Pitch/Miring Vertikal) -> Dikontrol oleh Posisi Z

    [Tooltip("Rotasi Y maksimum saat pemain mencapai PlayerMaxX (nilai positif) dan PlayerMinX (nilai negatif).")]
    public float maxRotationY = 15f; // Rotasi sumbu Y (Yaw/Banking) -> Dikontrol oleh Posisi X

    [Header("Smoothing")]
    [Tooltip("Kecepatan interpolasi rotasi. Nilai yang lebih tinggi menghasilkan gerakan yang lebih tajam.")]
    public float rotationSpeed = 5f;

    void Start()
    {
        // Cari Transform pemain dengan tag "Player" jika belum diatur
        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
            else
            {
                Debug.LogError("PlayerRotationController: Tidak dapat menemukan GameObject dengan tag 'Player'. Nonaktifkan skrip.");
                enabled = false;
            }
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        // 1. Ambil posisi pemain
        Vector3 currentPosition = playerTransform.position;

        // 2. Hitung Persentase Posisi (0.0 hingga 1.0)

        // Horizontal (X) - Digunakan untuk Rotasi Y (Yaw/Banking)
        float percentX = Mathf.InverseLerp(playerMinX, playerMaxX, currentPosition.x);

        // Kedalaman (Z) - Digunakan untuk Rotasi X (Pitch/Miring Vertikal)
        float percentZ = Mathf.InverseLerp(playerMinZ, playerMaxZ, currentPosition.z);
        // Catatan: Posisi Y pemain diabaikan dalam perhitungan ini, tetapi Anda dapat menggunakannya untuk Rotasi Z jika diperlukan.

        // 3. Petakan Persentase ke Rotasi (Interpolasi Linier)

        // Rotasi X: Rotasi X (Pitch) mengikuti Posisi Z pemain.
        // Saat MinZ (0%), rotasi harus -MaxRotationX. Saat MaxZ (100%), rotasi harus MaxRotationX.
        float targetRotationX = Mathf.Lerp(-maxRotationX, maxRotationX, percentZ);

        // Rotasi Y: Rotasi Y (Yaw) mengikuti Posisi X pemain.
        // Saat MinX (0%), rotasi harus -MaxRotationY. Saat MaxX (100%), rotasi harus MaxRotationY.
        float targetRotationY = Mathf.Lerp(-maxRotationY, maxRotationY, percentX);

        // 4. Hitung Rotasi Target Akhir
        // Rotasi Z disetel ke 0f (tidak ada Roll)
        // Rotasi Euler: (Pitch, Yaw, Roll) -> (targetRotationX, targetRotationY, 0f)
        Quaternion targetRotation = Quaternion.Euler(targetRotationX, targetRotationY, 0f);

        // 5. Terapkan Rotasi dengan Smoothing (Slerp)
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * rotationSpeed);
    }
}
