// GunSelector.cs (Updated with Tier Filtering based on Selected Vehicle)
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;

public class GunSelector : MonoBehaviour
{
    [Header("Menu Navigation")]
    [Tooltip("Reference to the VhcChgr script for menu navigation.")]
    public VhcChgr vhcChgr;

    [Header("Available Guns")]
    [Tooltip("List of all available Guns Scriptable Objects for the player to choose from.")]
    public Guns[] availableGuns;

    [Header("Item Generation Settings - NO LONGER USED FOR GENERATION")]
    private GameObject gunItemPrefab;

    [Tooltip("The RectTransform that holds the generated gun items (the Content Panel of the Scroll View).")]
    public RectTransform contentPanel;

    [Header("Current Selection")]
    [Tooltip("The Scriptable Object data for the gun currently selected in the menu.")]
    public Guns currentSelectedGun;

    private List<GunItemIdentifier> gunItems = new List<GunItemIdentifier>();

    [Header("UI Display References")]
    public TextMeshProUGUI gunNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI tierText;
    public TextMeshProUGUI priceText;
    public Image artworkImage;

    [Header("Snap Control")]
    [Tooltip("Reference to the SnapToItem script to initialize snapping after generation.")]
    public SnapToItem snapToItem;

    private Gun activeGunComponent;

    void OnEnable()
    {
        InitializeGunSelector();

        if (snapToItem != null)
        {
            snapToItem.gunSelector = this;
            snapToItem.payloadSelector = null;
        }
    }

    public void InitializeGunSelector()
    {
        if (contentPanel == null || snapToItem == null)
        {
            Debug.LogError("Content Panel or SnapToItem reference is missing on GunSelector.");
            return;
        }

        // 0. Ambil semua item identifier yang diletakkan secara manual
        gunItems = contentPanel.GetComponentsInChildren<GunItemIdentifier>(true).ToList();

        // 🌟 BARU: Deteksi Tier Maksimal yang diperbolehkan dari pesawat aktif
        int maxAllowedTier = 99; // Default jika tidak ditemukan kendaraan (bisa akses semua)
        if (vhcChgr != null && vhcChgr.CurrentVehicle != null)
        {
            maxAllowedTier = vhcChgr.CurrentVehicle.Tier;
            Debug.Log($"[GunSelector] Filtering guns for Vehicle: {vhcChgr.CurrentVehicle.name} (Max Tier: {maxAllowedTier})");
        }

        // 🌟 BARU: Saring visual item senjata. Aktifkan jika Tier <= maxAllowedTier, matikan jika lebih tinggi.
        for (int i = 0; i < gunItems.Count; i++)
        {
            if (i < availableGuns.Length && availableGuns[i] != null)
            {
                if (availableGuns[i].Tier <= maxAllowedTier)
                {
                    gunItems[i].gameObject.SetActive(true);
                }
                else
                {
                    gunItems[i].gameObject.SetActive(false); // Sembunyikan item tier tinggi
                }
            }
            else
            {
                gunItems[i].gameObject.SetActive(false); // Sembunyikan jika melebihi panjang array data
            }
        }

        // 1. Rekonstruksi layout agar ukuran konten langsung sinkron setelah ada item yang disembunyikan
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentPanel);

        
            
        // 3. Modifikasi Pengambilan itemPrefab: Ambil dari anak pertama yang berstatus AKTIF (valid)
        RectTransform validItemPrefab = null;
        for (int i = 0; i < contentPanel.childCount; i++)
        {
            Transform child = contentPanel.GetChild(i);
            if (child.gameObject.activeSelf)
            {
                validItemPrefab = child.GetComponent<RectTransform>();
                break;
            }
        }

        // Fallback jika tidak ada yang aktif
        if (validItemPrefab == null && contentPanel.childCount > 0)
        {
            validItemPrefab = contentPanel.GetChild(0).GetComponent<RectTransform>();
        }

        if (validItemPrefab != null)
        {
            snapToItem.itemPrefab = validItemPrefab;
        }
        else
        {
            Debug.LogError("Content Panel contains no valid child items for SnapToItem initialization.");
        }

        // Setelah semua SetActive selesai
        snapToItem.UpdateActiveItems();

        // 2. Set referensi SnapToItem
        snapToItem.gunSelector = this;

        // 5. Cari indeks pertama senjata yang valid/diperbolehkan (bukan asal indeks 0)
        int firstValidIndex = -1;
        for (int i = 0; i < availableGuns.Length; i++)
        {
            if (availableGuns[i] != null && availableGuns[i].Tier <= maxAllowedTier && i < gunItems.Count)
            {
                firstValidIndex = i;
                break;
            }
        }

        // RESET SCROLL POSITION FIRST
        contentPanel.localPosition = new Vector3(
            0,
            contentPanel.localPosition.y,
            contentPanel.localPosition.z
        );

        snapToItem.scrollRect.velocity = Vector2.zero;

        // Jika ada senjata legal
        if (firstValidIndex != -1)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentPanel);

            snapToItem.UpdateActiveItems();

            snapToItem.gunSelector = this;

            // RESET SCROLL STATE
            contentPanel.localPosition = new Vector3(
                0,
                contentPanel.localPosition.y,
                contentPanel.localPosition.z
            );

            snapToItem.scrollRect.velocity = Vector2.zero;

            // SNAP
            snapToItem.OnItemClick(firstValidIndex);

            // FORCE SELECT
            SetSelectedIndex(firstValidIndex);
            SelectGunByIndex(firstValidIndex);
        }
        else
        {
            Debug.LogError("No Guns available at or below the selected vehicle's tier.");
        }
    }

    public void SetSelectedIndex(int index)
    {
        for (int i = 0; i < gunItems.Count; i++)
        {
            bool isCurrent = (i == index);
            gunItems[i].SetSelectedStatus(isCurrent);
        }
    }

    public void SelectGunByIndex(int index)
    {
        if (index < 0 || index >= availableGuns.Length)
        {
            Debug.LogWarning($"Attempted to select gun at invalid index: {index}.");
            return;
        }

        Guns gunData = availableGuns[index];
        if (gunData == null)
        {
            Debug.LogError($"Gun data at index {index} is null.");
            return;
        }

        currentSelectedGun = gunData;
        FindAndApplyGun();
        UpdateGunDisplay(currentSelectedGun);
    }

    private void FindAndApplyGun()
    {
        if (activeGunComponent != null)
        {
            activeGunComponent.ApplyGunProperties(currentSelectedGun);
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            activeGunComponent = playerObj.GetComponentInChildren<Gun>();
        }

        if (activeGunComponent == null)
        {
            activeGunComponent = FindObjectOfType<Gun>();
        }

        if (activeGunComponent != null)
        {
            activeGunComponent.ApplyGunProperties(currentSelectedGun);
        }
        else
        {
            Debug.LogWarning("GunSelector could not find an active 'Gun' component in the scene to apply properties to.");
        }
    }

    private void UpdateGunDisplay(Guns gunData)
    {
        if (gunNameText != null) gunNameText.text = gunData.name;
        if (descriptionText != null) descriptionText.text = gunData.description;
        if (tierText != null) tierText.text = $"Tier: {gunData.Tier}";
        if (priceText != null) priceText.text = $"Price: {gunData.Price}";

        if (artworkImage != null)
        {
            artworkImage.sprite = gunData.artwork;
            artworkImage.enabled = (gunData.artwork != null);
        }
    }

    public void ConfirmGunSelectionAndGoBack(int indexToConfirm)
    {
        if (snapToItem == null)
        {
            Debug.LogError("SnapToItem reference is missing. Cannot perform confirmation logic.");
            return;
        }

        int currentSnappedIndex = -1;
        if (currentSelectedGun != null)
        {
            currentSnappedIndex = System.Array.IndexOf(availableGuns, currentSelectedGun);
        }

        if (indexToConfirm != currentSnappedIndex)
        {
            Debug.Log($"Mencoba konfirmasi item {indexToConfirm}, tapi item yang tersnap adalah {currentSnappedIndex}. Melakukan snap terlebih dahulu.");
            snapToItem.OnItemClick(indexToConfirm);
            return;
        }

        bool isCurrentItemSelected = gunItems.Count > currentSnappedIndex && gunItems[currentSnappedIndex].IsSelected;

        if (!isCurrentItemSelected)
        {
            Debug.LogWarning("Please select a gun by snapping to it before confirming.");
            return;
        }

        if (vhcChgr == null)
        {
            Debug.LogError("VhcChgr reference is missing. Cannot return to the previous menu.");
            return;
        }

        if (currentSelectedGun == null)
        {
            Debug.LogWarning("No gun selected to confirm.");
        }
        else
        {
            if (GameSelectionManager.Instance != null)
            {
                GameSelectionManager.Instance.SetConfirmedGun(currentSelectedGun);
            }
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX("Snap");
            }
            else
            {
                Debug.LogError("GameSelectionManager instance not found. Cannot persist gun selection.");
            }

            Debug.Log($"Gun selection confirmed: {currentSelectedGun.name}. Returning to Loadout Menu.");
        }

        vhcChgr.GoBackInMenu();
    }
}