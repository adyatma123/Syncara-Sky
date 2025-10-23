using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Manages the Payload Selection menu, allowing players to assign a Payload
/// ScriptableObject to a specific slot index.
/// </summary>
public class PayloadSelector : MonoBehaviour
{
    [Header("Menu Navigation")]
    [Tooltip("Reference to the VhcChgr script for menu navigation.")]
    public VhcChgr vhcChgr;

    [Header("Available Payloads")]
    [Tooltip("List of all available Payload Scriptable Objects.")]
    public Payload[] availablePayloads;

    [Header("Slot Management (Loadout Panel)")]
    [Tooltip("The parent RectTransform for the dynamically generated payload slot buttons.")]
    public RectTransform slotButtonContainer;
    [Tooltip("Prefab for the individual payload slot button.")]
    public GameObject slotButtonPrefab;

    // UI untuk pemilihan payload
    [Header("Payload Item Config Panel")]
    [Tooltip("Panel yang berisi scroll view, stats, dan tombol confirm.")]
    public RectTransform payloadSelectionPanel;
    [Tooltip("Content panel scroll view payload (tempat item UI manual berada).")]
    public RectTransform payloadItemsContent;

    [Header("UI Display References")]
    public TextMeshProUGUI payloadNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI tierText;
    public TextMeshProUGUI priceText;
    public Image artworkImage;

    [Header("Snap Control")]
    [Tooltip("Reference to the SnapToItem script for payload selection.")]
    public SnapToItem snapToItem;

    // Runtime data
    private int currentSlotIndex = -1; // Index slot payload yang sedang diedit (0, 1, 2, ...)
    private Payload currentSelectedPayload; // Payload yang saat ini terpilih di scroll view
    private List<PayloadItemIdentifier> payloadItems = new List<PayloadItemIdentifier>();
    private List<GameObject> slotButtons = new List<GameObject>();


    // --- Lifecycle and Initialization ---

    void OnEnable()
    {
        if (snapToItem != null)
        {
            snapToItem.payloadSelector = this;
            snapToItem.gunSelector = null; // pastikan gunSelector dimatikan
        }

        // Aktifkan panel slot selection default
        slotButtonContainer.gameObject.SetActive(true);
        payloadSelectionPanel.gameObject.SetActive(false);

        InitializePayloadItems();
        InitializeSlotButtons();
    }

    /// <summary>
    /// Find and link all manually placed PayloadItemIdentifier components 
    /// under the payloadItemsContent.
    /// </summary>
    private void InitializePayloadItems()
    {
        if (payloadItemsContent == null)
        {
            Debug.LogError("Payload Items Content RectTransform is missing.");
            return;
        }

        // Ambil semua PayloadItemIdentifier yang ada di bawah Content Panel
        payloadItems = payloadItemsContent.GetComponentsInChildren<PayloadItemIdentifier>(true).ToList();

        if (payloadItems.Count == 0)
        {
            Debug.LogWarning("No PayloadItemIdentifier components found in the Content Panel. Ensure items are placed manually.");
        }

        // Link data dari availablePayloads ke setiap item UI
        LinkManuallyPlacedPayloadItems();
    }

    /// <summary>
    /// Links the Payload Scriptable Objects to the manually placed UI items.
    /// </summary>
    private void LinkManuallyPlacedPayloadItems()
    {
        for (int i = 0; i < payloadItems.Count; i++)
        {
            if (i < availablePayloads.Length)
            {
                Payload data = availablePayloads[i];

                // Panggil Initialize pada setiap item identifier
                payloadItems[i].Initialize(this, i, data);

                // Pastikan item-item ini aktif (jika prefab/item diatur mati)
                payloadItems[i].gameObject.SetActive(true);
            }
            else
            {
                // Jika ada lebih banyak item UI daripada data, sembunyikan item UI tersebut
                payloadItems[i].gameObject.SetActive(false);
                // Debug.LogWarning($"More UI Payload items ({payloadItems.Count}) exist than available Payloads ({availablePayloads.Length}). Hiding item at index {i}.");
            }
        }

        // CRITICAL: Set SnapToItem's itemPrefab reference from the first child
        if (payloadItems.Count > 0)
        {
            RectTransform firstChild = payloadItems[0].GetComponent<RectTransform>();
            if (firstChild != null)
            {
                snapToItem.itemPrefab = firstChild;
            }
        }
    }

    /// <summary>
    /// Dynamic generation of slot buttons based on the vehicle's payload slot count.
    /// </summary>
    public void InitializeSlotButtons()
    {
        // Pastikan prefab referensi tidak null
        if (slotButtonPrefab == null)
        {
            Debug.LogError("Slot Button Prefab reference is missing in PayloadSelector!");
            return;
        }

        // 🔒 Pastikan prefab referensi disembunyikan dari UI
        if (slotButtonPrefab.activeSelf)
        {
            slotButtonPrefab.SetActive(false);
            Debug.Log("[PayloadSelector] Hiding slotButtonPrefab reference to avoid duplicate UI.");
        }

        // Bersihkan tombol lama
        foreach (GameObject button in slotButtons)
        {
            if (button != null)
                Destroy(button);
        }
        slotButtons.Clear();

        // Ambil jumlah slot dari GameSelectionManager
        int slotCount = GameSelectionManager.Instance != null
                        ? GameSelectionManager.Instance.VehiclePayloadSlotCount
                        : 4; // fallback

        // Buat tombol baru secara dinamis
        for (int i = 0; i < slotCount; i++)
        {
            GameObject newButton = Instantiate(slotButtonPrefab, slotButtonContainer);
            newButton.name = $"SlotButton_{i}";
            newButton.SetActive(true); // aktifkan hasil clone agar terlihat di UI

            // Dapatkan komponen Button dan Text
            Button buttonComponent = newButton.GetComponent<Button>();
            TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonComponent != null)
            {
                int index = i; // closure fix
                buttonComponent.onClick.AddListener(() => OnSlotButtonClick(index));
            }

            if (buttonText != null)
            {
                // Ambil payload yang sudah dikonfirmasi untuk slot ini
                Payload existingPayload = GameSelectionManager.Instance.ConfirmedPayloadSelections[i];
                buttonText.text = $"{(existingPayload != null ? existingPayload.payloadName : $"SLOT {i + 1}\n")}";
            }

            slotButtons.Add(newButton);
        }

        // Force rebuild layout setelah membuat tombol
        LayoutRebuilder.ForceRebuildLayoutImmediate(slotButtonContainer);
    }

    // --- Slot Button Handling ---

    /// <summary>
    /// Dipanggil ketika salah satu tombol slot ditekan.
    /// </summary>
    /// <param name="slotIndex">Index dari slot yang ditekan.</param>
    public void OnSlotButtonClick(int slotIndex)
    {
        currentSlotIndex = slotIndex;
        SoundManager.Instance.PlaySFX("Click");

        // NEW: Gunakan MenuCameraController untuk mengelola transisi UI/Camera state
        // Memastikan MenuController dapat diakses melalui properti publik
        if (vhcChgr.MenuController != null)
        {
            vhcChgr.MenuController.TransitionToPayloadItemConfig(); // Pindah ke tampilan Item Selection
        }
        else
        {
            Debug.LogError("MenuController is not accessible on VhcChgr.");
        }

        // Inisialisasi tampilan seleksi payload
        InitializePayloadSelectionUI();
    }

    /// <summary>
    /// Didesain agar dipanggil oleh tombol 'Back' saat berada di tampilan seleksi payload.
    /// </summary>
    public void BackToSlotSelection()
    {
        currentSlotIndex = -1; // Reset index

        // NEW: Implementasikan payload ke PayloadManager pada 'Player'
        ImplementConfirmedPayloadToPlayer();

        // NEW: Gunakan MenuCameraController untuk mengelola transisi UI/Camera state
        if (vhcChgr.MenuController != null)
        {
            vhcChgr.MenuController.TransitionToPayloadSlotMenu(); // Kembali ke tampilan Slot Selection
        }
        else
        {
            Debug.LogError("MenuController is not accessible on VhcChgr.");
        }

        // Muat ulang tombol slot untuk menampilkan payload yang sudah dipilih
        InitializeSlotButtons();

        SoundManager.Instance.PlaySFX("Click");
    }

    /// <summary>
    /// PUBLIC: Dipanggil oleh VhcChgr.GoBackInMenu() untuk kembali ke menu sebelumnya (Loadout).
    /// HANYA dipanggil saat berada di tampilan Slot Selection.
    /// </summary>
    public void GoBackToLoadoutMenu()
    {
        // HANYA kembali jika kita berada di tampilan Slot Selection
        if (slotButtonContainer.gameObject.activeInHierarchy)
        {
            // NEW: Implementasikan payload ke PayloadManager sebelum kembali ke Loadout
            ImplementConfirmedPayloadToPlayer();

            vhcChgr.GoBackInMenu();
        }
        else
        {
            // Jika berada di tampilan Payload Selection, kembali ke Slot Selection
            BackToSlotSelection();
        }
    }

    /// <summary>
    /// NEW: Mencari PayloadManager di GameObject ber-tag 'Player' dan memperbarui loadout-nya.
    /// </summary>
    private void ImplementConfirmedPayloadToPlayer()
    {
        // 1. Cari GameObject dengan tag 'Player'
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogWarning("[PayloadSelector] GameObject dengan tag 'Player' tidak ditemukan. Gagal implementasi payload.");
            return;
        }

        // 2. Dapatkan komponen PayloadManager
        PayloadManager payloadManager = player.GetComponent<PayloadManager>();

        if (payloadManager == null)
        {
            Debug.LogWarning($"[PayloadSelector] Komponen PayloadManager tidak ditemukan pada GameObject '{player.name}'. Gagal implementasi payload.");
            return;
        }

        // 3. Update SEMUA slot pada PayloadManager, karena GameSelectionManager 
        //    memegang satu-satunya sumber kebenaran untuk loadout.
        Payload[] confirmedPayloads = GameSelectionManager.Instance.ConfirmedPayloadSelections;

        if (confirmedPayloads.Length != payloadManager.payloadSlots.Length)
        {
            Debug.LogError($"[PayloadSelector] Jumlah slot di GameSelectionManager ({confirmedPayloads.Length}) tidak cocok dengan jumlah slot fisik di Player ({payloadManager.payloadSlots.Length}). Gagal implementasi.");
            return;
        }

        // 4. Update setiap slot
        for (int i = 0; i < confirmedPayloads.Length; i++)
        {
            // Panggil metode pada PayloadManager untuk mengatur payload baru.
            // PayloadManager yang akan menangani ReinitializeLoadout/Processing.
            payloadManager.SetPayloadAtSlotIndex(i, confirmedPayloads[i]);
        }

        // 5. Setelah semua slot diatur, panggil ReinitializeLoadout satu kali (Opsional, sudah ada di SetPayloadAtSlotIndex)
        // payloadManager.ReinitializeLoadout();

        Debug.Log("[PayloadSelector] Loadout Payload telah diimplementasikan ke Player.");
    }


    // --- Payload Selection UI Handling ---

    private void InitializePayloadSelectionUI()
    {
        if (availablePayloads == null || availablePayloads.Length == 0)
        {
            Debug.LogError("No Payloads assigned to PayloadSelector.");
            return;
        }

        // Konfigurasi SnapToItem
        LayoutRebuilder.ForceRebuildLayoutImmediate(payloadItemsContent);
        snapToItem.payloadSelector = this; // Pastikan referensi SnapToItem terisi

        // Cari payload yang sudah dikonfirmasi di slot ini
        Payload existingPayload = GameSelectionManager.Instance.ConfirmedPayloadSelections[currentSlotIndex];
        int initialIndex = existingPayload != null ? System.Array.IndexOf(availablePayloads, existingPayload) : 0;

        // Pastikan initialIndex valid
        if (initialIndex < 0 || initialIndex >= availablePayloads.Length) initialIndex = 0;

        // Snap ke item yang sudah ada atau ke item 0
        snapToItem.OnItemClick(initialIndex);
        SetSelectedIndex(initialIndex);
        SelectPayloadByIndex(initialIndex);
    }


    /// <summary>
    /// Dipanggil oleh SnapToItem.Update() atau OnItemClick untuk mengatur status IsSelected.
    /// </summary>
    public void SetSelectedIndex(int index)
    {
        for (int i = 0; i < payloadItems.Count; i++)
        {
            payloadItems[i].SetSelectedStatus(i == index);
        }
    }

    /// <summary>
    /// PUBLIC: Called by SnapToItem when a snap is complete. Updates the main UI display.
    /// </summary>
    public void SelectPayloadByIndex(int index)
    {
        if (index < 0 || index >= availablePayloads.Length)
        {
            Debug.LogWarning($"[PayloadSelector] Invalid payload index {index}");
            return;
        }

        currentSelectedPayload = availablePayloads[index];
        SetSelectedIndex(index);

        Debug.Log($"[PayloadSelector] Snapped to payload: {currentSelectedPayload.payloadName} (Index {index})");

        // Perbarui UI kanan
        UpdatePayloadDisplay(currentSelectedPayload);
    }

    private void UpdatePayloadDisplay(Payload payloadData)
    {
        if (payloadNameText != null) payloadNameText.text = payloadData.payloadName;
        if (descriptionText != null) descriptionText.text = payloadData.payloadDescription;
        if (tierText != null) tierText.text = $"Tier: {payloadData.Tier}";
        if (priceText != null) priceText.text = $"Price: {payloadData.Price} (x{payloadData.maxAmmo})";

        if (artworkImage != null)
        {
            artworkImage.sprite = payloadData.artwork;
            artworkImage.enabled = (payloadData.artwork != null);
        }
    }

    /// <summary>
    /// PUBLIC: Dipanggil oleh tombol "CONFIRM" di tampilan seleksi payload.
    /// </summary>
    public void ConfirmPayloadSelection()
    {
        if (currentSlotIndex == -1)
        {
            Debug.LogError("Error: Confirmation attempted with no active slot index.");
            return;
        }

        if (snapToItem == null)
        {
            Debug.LogError("SnapToItem reference missing in PayloadSelector!");
            return;
        }

        // --- 1️⃣ Tentukan index payload yang sedang tersnap ---
        int currentSnappedIndex = -1;
        if (currentSelectedPayload != null)
            currentSnappedIndex = System.Array.IndexOf(availablePayloads, currentSelectedPayload);

        // --- 2️⃣ Cegah konfirmasi jika belum tersnap ke item yang benar ---
        if (currentSnappedIndex < 0 || currentSelectedPayload == null || !payloadItems[currentSnappedIndex].IsSelected)
        {
            Debug.Log($"[PayloadSelector] Belum tersnap ke payload yang benar. Snap ulang sebelum konfirmasi...");
            int safeIndex = Mathf.Clamp(currentSnappedIndex, 0, availablePayloads.Length - 1);
            snapToItem.OnItemClick(safeIndex);
            return;
        }

        // --- 3️⃣ Lanjutkan jika sudah tersnap dengan benar ---

        // --- 4️⃣ Simpan payload ke GameSelectionManager (Sumber Kebenaran) ---
        if (GameSelectionManager.Instance != null)
        {
            Payload[] confirmedPayloads = GameSelectionManager.Instance.ConfirmedPayloadSelections;

            if (confirmedPayloads.Length > currentSlotIndex)
            {
                confirmedPayloads[currentSlotIndex] = currentSelectedPayload;
                // GameSelectionManager.Instance.SetConfirmedPayloads(confirmedPayloads); // Tidak perlu dipanggil lagi karena array sudah diubah referensinya

                SoundManager.Instance.PlaySFX("Snap");
                Debug.Log($"[PayloadSelector] Payload dikonfirmasi: {currentSelectedPayload.payloadName} ke Slot {currentSlotIndex + 1}.");

                // NEW: Setelah disimpan ke GameSelectionManager, implementasikan ke Player
                ImplementConfirmedPayloadToPlayer();
            }
            else
            {
                Debug.LogError("[PayloadSelector] Index slot invalid atau jumlah slot tidak sesuai.");
            }
        }
        else
        {
            Debug.LogError("[PayloadSelector] GameSelectionManager.Instance tidak ditemukan.");
        }

        // --- 5️⃣ Kembali ke menu slot selection ---
        BackToSlotSelection();
    }
}
