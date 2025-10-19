using UnityEngine;
using UnityEngine.UI;
using System.Collections; // PENTING: Diperlukan untuk Coroutine

public class HeatBar : MonoBehaviour
{
    public RectTransform heatBarRect;
    public Image heatImage; // Assign your Image in the Inspector
    private Gun gun; // Variabel ini sekarang diisi setelah pesawat ditemukan

    private float initialHeight;
    private bool gunFound = false;

    [Range(0, 1)]
    public float heatImageAlpha = 1f;
    private Color heatColor;

    private void Start()
    {
        if (heatBarRect != null)
        {
            initialHeight = heatBarRect.rect.height;
        }
        else
        {
            Debug.LogError("Heat bar Rect Transform not assigned!");
        }

        // Memulai pencarian dengan sedikit penundaan (lebih andal daripada langsung di Start)
        StartCoroutine(FindGunAfterDelay());
    }

    // Coroutine untuk menunggu satu frame sebelum mencari objek
    IEnumerator FindGunAfterDelay()
    {
        // Tunggu satu frame. Ini memberikan waktu pada PlayerController.Start() 
        // untuk meng-instansiasi dan memberi tag "Player" pada pesawat.
        yield return null;

        // Ulangi pencarian sampai komponen ditemukan
        int maxAttempts = 5; // Batasi upaya pencarian untuk menghindari loop tak terbatas
        int currentAttempt = 0;

        while (!gunFound && currentAttempt < maxAttempts)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                // Menganggap komponen Gun berada langsung di GameObject "Player"
                gun = player.GetComponent<Gun>();

                if (gun != null)
                {
                    gunFound = true; // Berhenti mencari
                    Debug.Log("[HeatBar] Gun component successfully linked.");
                }
                else
                {
                    Debug.LogError("[HeatBar] Gun script not found on Player!");
                }
            }
            // Jika objek Player belum ditemukan, tunggu frame berikutnya
            yield return null;
            currentAttempt++;
        }

        if (!gunFound)
        {
            Debug.LogError("[HeatBar] Failed to find Gun component after multiple attempts!");
            enabled = false; // Nonaktifkan skrip jika gagal
        }
    }

    private void Update()
    {
        // Hanya jalankan update jika sudah ditemukan
        if (!gunFound) return;

        if (heatBarRect != null && gun != null)
        {
            float heat = gun.currentHeat;
            float maxHeat = gun.maxHeat;

            // Update bar height
            float heatRatio = heat / maxHeat;
            float newHeight = initialHeight * (1f - heatRatio);
            newHeight = Mathf.Clamp(newHeight, 0f, initialHeight);

            heatBarRect.sizeDelta = new Vector2(heatBarRect.sizeDelta.x, newHeight);

            // Update color logic
            float lerpFactor;
            if (heat <= maxHeat / 2f)
            {
                // White/Green to Orange/Yellow
                lerpFactor = heat / (maxHeat / 2f);
                // Ubah Color.white menjadi Color.green jika Anda ingin tampilan defaultnya hijau
                heatColor = Color.Lerp(Color.white, new Color(1f, 0.5f, 0f), lerpFactor);
            }
            else
            {
                // Orange/Yellow to Red
                lerpFactor = (heat - (maxHeat / 2f)) / (maxHeat / 2f);
                heatColor = Color.Lerp(new Color(1f, 0.5f, 0f), Color.red, lerpFactor);
            }

            heatColor.a = heatImageAlpha;
            heatImage.color = heatColor;
        }
    }
}
