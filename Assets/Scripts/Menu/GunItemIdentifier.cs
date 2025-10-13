using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Diletakkan pada item UI GunItem untuk mendeteksi indeksnya, menyimpan data, 
/// dan menampilkan status seleksi (IsSelected).
/// </summary>
public class GunItemIdentifier : MonoBehaviour
{
    private GunSelector gunSelector;

    [Tooltip("Indeks item ini dalam array availableGuns.")]
    public int ItemIndex = -1;

    // NEW: Toggle untuk status seleksi
    [Tooltip("Indikator visual bahwa item ini sedang dipilih.")]
    public bool IsSelected { get; private set; } = false;

    [Tooltip("Komponen yang akan diaktifkan/dinonaktifkan saat item terpilih (opsional, misalnya border atau highlight).")]
    public GameObject SelectionHighlight; // Contoh: Highlight border

    void Awake()
    {
        // Tentukan indeks item ini berdasarkan urutannya di dalam Content Panel
        ItemIndex = transform.GetSiblingIndex();

        // Menonaktifkan highlight secara default
        if (SelectionHighlight != null)
        {
            SelectionHighlight.SetActive(false);
        }

        // Tambahkan listener untuk Button agar SnapToItem dipanggil
        Button itemButton = GetComponent<Button>();
        if (itemButton != null)
        {
            gunSelector = FindObjectOfType<GunSelector>();
            if (gunSelector != null && gunSelector.snapToItem != null)
            {
                // Hapus listener lama jika ada
                itemButton.onClick.RemoveAllListeners();

                // Panggil SnapToItem.OnItemClick
                itemButton.onClick.AddListener(() => gunSelector.snapToItem.OnItemClick(ItemIndex));

                // Aplikasikan data UI (jika Anda ingin melakukannya di sini)
                ApplyUIData(gunSelector);
            }
        }
    }

    /// <summary>
    /// Dipanggil oleh GunSelector untuk mengatur status seleksi item ini.
    /// </summary>
    /// <param name="status">True jika item ini yang terpilih/ter-snap.</param>
    public void SetSelectedStatus(bool status)
    {
        IsSelected = status;

        if (SelectionHighlight != null)
        {
            SelectionHighlight.SetActive(status);
        }

        // Logika visual lain (misalnya, mengubah warna teks/gambar) bisa ditambahkan di sini
    }

    /// <summary>
    /// Mengaplikasikan data ScriptableObject ke komponen UI (Misalnya Text Legacy).
    /// </summary>
    private void ApplyUIData(GunSelector selector)
    {
        if (selector.availableGuns == null || ItemIndex >= selector.availableGuns.Length) return;

        Guns gunData = selector.availableGuns[ItemIndex];

        // Cari Text (Legacy) di anak item
        Text itemLegacyText = GetComponentInChildren<Text>();
        if (itemLegacyText != null)
        {
            itemLegacyText.text = gunData.name;
        }
        else
        {
            Debug.LogWarning($"Item {gameObject.name} (Index: {ItemIndex}) tidak memiliki komponen Text (Legacy).");
        }
    }
}
