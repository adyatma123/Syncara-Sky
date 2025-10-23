//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// Skrip ini dilekatkan pada tombol UI yang mewakili satu slot Payload pada kendaraan.
///// Catatan: OnClick() HARUS diset secara manual di Prefab/Inspector untuk memanggil SetActiveSlot().
///// </summary>
//public class SlotButtonIdentifier : MonoBehaviour
//{
//    [Header("UI References")]
//    public TextMeshProUGUI slotNameText;
//    public TextMeshProUGUI currentPayloadText;
//    public GameObject selectionIndicator;

//    // FIELDS PRIVATE (Diset oleh PayloadSelector saat Generate)
//    // Nilai ini akan diset secara otomatis saat tombol digenerate.
//    private int slotIndex;
//    private PayloadSelector payloadSelector;

//    private Button button;
//    private Image buttonImage; // NEW: Menyimpan referensi Image

//    void Awake()
//    {
//        button = GetComponent<Button>();
//        buttonImage = GetComponent<Image>(); // NEW: Ambil komponen Image di tombol
//        // PENTING: Tidak ada onClick.AddListener() di sini. Harus diset manual di Inspector.
//    }

//    /// <summary>
//    /// Dipanggil oleh PayloadSelector saat tombol digenerate.
//    /// </summary>
//    /// <param name="index">Index slot yang diinisialisasi.</param>
//    /// <param name="selector">Reference PayloadSelector.</param>
//    public void Initialize(int index, PayloadSelector selector)
//    {
//        // Menyimpan referensi runtime yang dibutuhkan
//        slotIndex = index;
//        payloadSelector = selector;

//        // --- FIX: AKTIFKAN KOMPONEN UI DI CHILD SECARA PAKSA ---
//        if (slotNameText != null)
//        {
//            slotNameText.text = $"SLOT {index + 1}";
//            slotNameText.enabled = true; // Fix: Pastikan Teks Aktif
//        }

//        if (currentPayloadText != null)
//        {
//            currentPayloadText.enabled = true; // Fix: Pastikan Teks Aktif
//        }

//        if (buttonImage != null)
//        {
//            buttonImage.enabled = true; // FIX: Pastikan Image/Latar Belakang Tombol Aktif
//        }
//        // --- END FIX ---

//        SetSelectedStatus(false);
//    }

//    /// <summary>
//    /// Digunakan secara eksternal untuk memperbarui visual tombol.
//    /// </summary>
//    public void UpdatePayloadDisplay(Payload payloadData)
//    {
//        if (currentPayloadText != null)
//        {
//            if (payloadData != null)
//            {
//                currentPayloadText.text = payloadData.payloadName;
//            }
//            else
//            {
//                currentPayloadText.text = "EMPTY";
//            }
//        }
//    }

//    /// <summary>
//    /// Mengubah status visual seleksi tombol.
//    /// </summary>
//    public void SetSelectedStatus(bool isSelected)
//    {
//        if (selectionIndicator != null)
//        {
//            selectionIndicator.SetActive(isSelected);
//        }
//        if (currentPayloadText != null)
//        {
//            // Opsional: Ganti warna teks saat terpilih
//            currentPayloadText.color = isSelected ? Color.yellow : Color.white;
//        }
//    }


//    /// <summary>
//    /// PUBLIC: Dipanggil dari Unity Inspector OnClick() saat tombol slot ditekan.
//    /// </summary>
//    public void SetActiveSlot()
//    {
//        if (payloadSelector != null)
//        {
//            // Panggil fungsi di PayloadSelector dengan index yang diset saat Initialize
//            payloadSelector.SetActiveSlotIndex(slotIndex);
//        }
//        else
//        {
//            Debug.LogError($"PayloadSelector reference is missing for slot index {slotIndex}. Cannot set active slot.");
//        }
//    }
//}
