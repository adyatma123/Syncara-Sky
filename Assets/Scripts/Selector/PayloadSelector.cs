// PayloadSelector.cs (Updated with Tier Filtering based on Selected Vehicle)
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
    /// under the payloadItemsContent. Includes Tier filtering.
    /// </summary>
    public void InitializePayloadItems()
    {
        if (payloadItemsContent == null)
        {
            Debug.LogError("Payload Items Content RectTransform is missing.");
            return;
        }

        // Ambil semua PayloadItemIdentifier yang ada di bawah Content Panel
        payloadItems = payloadItemsContent.GetComponentsInChildren<PayloadItemIdentifier>(true).ToList();

        // 🌟 BARU: Deteksi Tier Maksimal yang diperbolehkan dari pesawat aktif
        int maxAllowedTier = 99; // Default jika tidak ditemukan kendaraan (bisa akses semua)
        if (vhcChgr != null && vhcChgr.CurrentVehicle != null)
        {
            maxAllowedTier = vhcChgr.CurrentVehicle.Tier;
            Debug.Log($"[PayloadSelector] Filtering payloads for Vehicle: {vhcChgr.CurrentVehicle.name} (Max Tier: {maxAllowedTier})");
        }

        if (payloadItems.Count == 0)
        {
            Debug.LogWarning("No PayloadItemIdentifier components found in the Content Panel. Ensure items are placed manually.");
        }

        // Link data dari availablePayloads ke setiap item UI dengan filter Tier
        for (int i = 0; i < payloadItems.Count; i++)
        {
            if (i < availablePayloads.Length && availablePayloads[i] != null)
            {
                // Saring berdasarkan Tier kendaraan saat ini
                if (availablePayloads[i].Tier <= maxAllowedTier)
                {
                    payloadItems[i].Initialize(this, i, availablePayloads[i]);
                    payloadItems[i].gameObject.SetActive(true); // Aktifkan item legal
                }
                else
                {
                    payloadItems[i].gameObject.SetActive(false); // Sembunyikan item tier tinggi
                }
            }
            else
            {
                // Jika ada lebih banyak item UI daripada data, sembunyikan item UI tersebut
                payloadItems[i].gameObject.SetActive(false);
            }
        }

        // 🌟 BARU: Ambil komponen anak pertama yang AKTIF (valid) untuk dijadikan itemPrefab acuan SnapToItem
        RectTransform validItemPrefab = null;
        for (int i = 0; i < payloadItemsContent.childCount; i++)
        {
            Transform child = payloadItemsContent.GetChild(i);
            if (child.gameObject.activeSelf)
            {
                validItemPrefab = child.GetComponent<RectTransform>();
                break;
            }
        }

        // Fallback jika tidak ada yang aktif
        if (validItemPrefab == null && payloadItems.Count > 0)
        {
            validItemPrefab = payloadItems[0].GetComponent<RectTransform>();
        }

        if (validItemPrefab != null)
        {
            snapToItem.itemPrefab = validItemPrefab;
        }
    }

    /// <summary>
    /// Links the Payload Scriptable Objects to the manually placed UI items.
    /// (LOGIC MOVED DIRECTLY INTO INITIALIZEPAYLOADITEMS TO SUPPORT TIER FILTERING)
    /// </summary>
    private void LinkManuallyPlacedPayloadItems()
    {
        // Diabaikan karena logikanya sudah disatukan di InitializePayloadItems agar filter berjalan optimal
    }

    /// <summary>
    /// Dynamic generation of slot buttons based on the vehicle's payload slot count.
    /// </summary>
    public void InitializeSlotButtons()
    {
        if (slotButtonPrefab == null)
        {
            Debug.LogError("Slot Button Prefab reference is missing in PayloadSelector!");
            return;
        }

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

        int slotCount = GameSelectionManager.Instance != null
                        ? GameSelectionManager.Instance.VehiclePayloadSlotCount
                        : 4; // fallback

        for (int i = 0; i < slotCount; i++)
        {
            GameObject newButton = Instantiate(slotButtonPrefab, slotButtonContainer);
            newButton.name = $"SlotButton_{i}";
            newButton.SetActive(true);

            Button buttonComponent = newButton.GetComponent<Button>();
            TextMeshProUGUI buttonText = newButton.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonComponent != null)
            {
                int index = i;
                buttonComponent.onClick.AddListener(() => OnSlotButtonClick(index));
            }

            if (buttonText != null)
            {
                Payload existingPayload = GameSelectionManager.Instance.ConfirmedPayloadSelections[i];
                buttonText.text = $"{(existingPayload != null ? existingPayload.payloadName : $"SLOT {i + 1}\n")}";
            }

            slotButtons.Add(newButton);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(slotButtonContainer);
    }

    // --- Slot Button Handling ---

    public void OnSlotButtonClick(int slotIndex)
    {
        currentSlotIndex = slotIndex;
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("Click");
        }
        if (vhcChgr.MenuController != null)
        {
            vhcChgr.MenuController.TransitionToPayloadItemConfig();
        }
        else
        {
            Debug.LogError("MenuController is not accessible on VhcChgr.");
        }

        InitializePayloadSelectionUI();
    }

    public void BackToSlotSelection()
    {
        currentSlotIndex = -1;

        ImplementConfirmedPayloadToPlayer();

        if (vhcChgr.MenuController != null)
        {
            vhcChgr.MenuController.TransitionToPayloadSlotMenu();
        }
        else
        {
            Debug.LogError("MenuController is not accessible on VhcChgr.");
        }

        InitializeSlotButtons();
        SoundManager.Instance.PlaySFX("Click");
    }

    public void GoBackToLoadoutMenu()
    {
        if (slotButtonContainer.gameObject.activeInHierarchy)
        {
            ImplementConfirmedPayloadToPlayer();
            vhcChgr.GoBackInMenu();
        }
        else
        {
            BackToSlotSelection();
        }
    }

    private void ImplementConfirmedPayloadToPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[PayloadSelector] GameObject dengan tag 'Player' tidak ditemukan. Gagal implementasi payload.");
            return;
        }

        PayloadManager payloadManager = player.GetComponent<PayloadManager>();
        if (payloadManager == null)
        {
            Debug.LogWarning($"[PayloadSelector] Komponen PayloadManager tidak ditemukan pada GameObject '{player.name}'. Gagal implementasi payload.");
            return;
        }

        Payload[] confirmedPayloads = GameSelectionManager.Instance.ConfirmedPayloadSelections;
        if (confirmedPayloads.Length != payloadManager.payloadSlots.Length)
        {
            Debug.LogError($"[PayloadSelector] Jumlah slot di GameSelectionManager ({confirmedPayloads.Length}) tidak cocok dengan jumlah slot fisik di Player ({payloadManager.payloadSlots.Length}). Gagal implementasi.");
            return;
        }

        for (int i = 0; i < confirmedPayloads.Length; i++)
        {
            payloadManager.SetPayloadAtSlotIndex(i, confirmedPayloads[i]);
        }

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

        LayoutRebuilder.ForceRebuildLayoutImmediate(payloadItemsContent);

        snapToItem.UpdateActiveItems();

        snapToItem.payloadSelector = this;

        // Ambil batasan tier kendaraan saat ini
        int maxAllowedTier = vhcChgr != null && vhcChgr.CurrentVehicle != null ? vhcChgr.CurrentVehicle.Tier : 99;

        Payload existingPayload = GameSelectionManager.Instance.ConfirmedPayloadSelections[currentSlotIndex];
        int initialIndex = existingPayload != null ? System.Array.IndexOf(availablePayloads, existingPayload) : -1;

        // 🌟 BARU: Jika item yang sebelumnya disimpan ternyata melebihi tier (tidak valid) atau belum ada, cari indeks legal pertama
        if (initialIndex == -1 || availablePayloads[initialIndex].Tier > maxAllowedTier)
        {
            initialIndex = -1;
            for (int i = 0; i < availablePayloads.Length; i++)
            {
                if (availablePayloads[i] != null && availablePayloads[i].Tier <= maxAllowedTier && i < payloadItems.Count)
                {
                    initialIndex = i;
                    break;
                }
            }
        }

        // Jika tidak ada payload yang legal sama sekali
        if (initialIndex == -1)
        {
            Debug.LogError("No Payloads available at or below the selected vehicle's tier.");
            return;
        }

        // Jalankan snapping awal ke item legal pertama/terpilih
        snapToItem.OnItemClick(initialIndex);
        SetSelectedIndex(initialIndex);
        SelectPayloadByIndex(initialIndex);
    }

    public void SetSelectedIndex(int index)
    {
        for (int i = 0; i < payloadItems.Count; i++)
        {
            payloadItems[i].SetSelectedStatus(i == index);
        }
    }

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

        int currentSnappedIndex = -1;
        if (currentSelectedPayload != null)
            currentSnappedIndex = System.Array.IndexOf(availablePayloads, currentSelectedPayload);

        if (currentSnappedIndex < 0 || currentSelectedPayload == null || !payloadItems[currentSnappedIndex].IsSelected)
        {
            Debug.Log($"[PayloadSelector] Belum tersnap ke payload yang benar. Snap ulang sebelum konfirmasi...");
            int safeIndex = Mathf.Clamp(currentSnappedIndex, 0, availablePayloads.Length - 1);
            snapToItem.OnItemClick(safeIndex);
            return;
        }

        if (GameSelectionManager.Instance != null)
        {
            Payload[] confirmedPayloads = GameSelectionManager.Instance.ConfirmedPayloadSelections;

            if (confirmedPayloads.Length > currentSlotIndex)
            {
                confirmedPayloads[currentSlotIndex] = currentSelectedPayload;

                SoundManager.Instance.PlaySFX("Snap");
                Debug.Log($"[PayloadSelector] Payload dikonfirmasi: {currentSelectedPayload.payloadName} ke Slot {currentSlotIndex + 1}.");

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

        BackToSlotSelection();
    }
}