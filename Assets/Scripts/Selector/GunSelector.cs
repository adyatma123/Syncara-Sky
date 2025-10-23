using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Manages the Gun Selection menu. Holds an array of available Guns Scriptable Objects
/// and applies the selected properties to the active Gun component on the player's vehicle.
/// NOW EXPECTS GUN ITEMS TO BE MANUALLY PLACED IN THE CONTENT PANEL.
/// </summary>
public class GunSelector : MonoBehaviour
{
    [Header("Menu Navigation")]
    [Tooltip("Reference to the VhcChgr script for menu navigation.")]
    public VhcChgr vhcChgr;

    [Header("Available Guns")]
    [Tooltip("List of all available Guns Scriptable Objects for the player to choose from.")]
    public Guns[] availableGuns;

    [Header("Item Generation Settings - NO LONGER USED FOR GENERATION")]
    // This field is no longer used for instantiation and is kept for reference/cleanup.
    private GameObject gunItemPrefab;

    [Tooltip("The RectTransform that holds the generated gun items (the Content Panel of the Scroll View).")]
    public RectTransform contentPanel;


    [Header("Current Selection")]
    [Tooltip("The Scriptable Object data for the gun currently selected in the menu.")]
    public Guns currentSelectedGun;

    // NEW: List untuk melacak semua GunItemIdentifier
    private List<GunItemIdentifier> gunItems = new List<GunItemIdentifier>();


    [Header("UI Display References")]
    // References to UI elements to show the selected gun's stats
    public TextMeshProUGUI gunNameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI tierText;
    public TextMeshProUGUI priceText;
    public Image artworkImage;

    [Header("Snap Control")]
    [Tooltip("Reference to the SnapToItem script to initialize snapping after generation.")]
    public SnapToItem snapToItem;

    private Gun activeGunComponent;

    // Use OnEnable to re-initialize when the menu becomes active again (fixes chaotic layout on re-entry)
    void OnEnable()
    {
        InitializeGunSelector();

        if (snapToItem != null)
        {
            snapToItem.gunSelector = this;
            snapToItem.payloadSelector = null; // pastikan payloadSelector dimatikan
        }
    }

    void InitializeGunSelector()
    {
        if (contentPanel == null || snapToItem == null)
        {
            Debug.LogError("Content Panel or SnapToItem reference is missing on GunSelector.");
            return;
        }

        // 0. Populate the list of item identifiers
        gunItems = contentPanel.GetComponentsInChildren<GunItemIdentifier>(true).ToList();

        // 1. Force a layout rebuild to ensure all items are scaled correctly BEFORE linking/snapping
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentPanel);

        // 2. CRITICAL FIX: Set SnapToItem's gunSelector reference 
        snapToItem.gunSelector = this;

        // 3. CRITICAL FIX: Ensure SnapToItem gets its itemPrefab REFERENCE from the first child.
        if (contentPanel.childCount > 0)
        {
            RectTransform firstChild = contentPanel.GetChild(0).GetComponent<RectTransform>();
            if (firstChild != null)
            {
                snapToItem.itemPrefab = firstChild;
            }
            else
            {
                Debug.LogError("First child in Content Panel is missing a RectTransform. Cannot initialize SnapToItem.");
            }
        }

        // 4. LinkManuallyPlacedGunItems() Dihapus - Logic sekarang ada di GunItemIdentifier.Awake()

        // 5. Check for available guns and initialize display
        if (availableGuns.Length > 0)
        {
            if (snapToItem.itemPrefab != null)
            {
                snapToItem.OnItemClick(0); // Snap to the first item (index 0)
            }

            // SetSelectedIndex dipanggil untuk memastikan visual highlight item 0 aktif
            SetSelectedIndex(0);
            SelectGunByIndex(0);
        }
        else
        {
            Debug.LogError("No Guns Scriptable Objects assigned to GunSelector. Please assign them in the Inspector.");
        }
    }


    // HAPUS atau komentar seluruh metode LinkManuallyPlacedGunItems
    /*
    private void LinkManuallyPlacedGunItems()
    {
        // ... (Logika lama untuk link data dan tombol)
    }
    */

    /// <summary>
    /// NEW: Dipanggil oleh SnapToItem.Update() atau OnItemClick untuk mengatur status IsSelected.
    /// </summary>
    /// <param name="index">Indeks item yang harus disetel menjadi terpilih (true).</param>
    public void SetSelectedIndex(int index)
    {
        for (int i = 0; i < gunItems.Count; i++)
        {
            bool isCurrent = (i == index);
            gunItems[i].SetSelectedStatus(isCurrent);
        }
    }


    /// <summary>
    /// PUBLIC: Called by SnapToItem when a snap is complete (or initially). Updates the main UI display.
    /// </summary>
    /// <param name="index">The index of the gun to select.</param>
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

        // Immediately apply the new gun properties to the active Gun component (for preview only)
        FindAndApplyGun();

        // Update the menu display
        UpdateGunDisplay(currentSelectedGun);

    }

    // This method is now private and ONLY used internally. SelectGunByIndex is the public access point.
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

    /// <summary>
    /// CONFIRM: Hanya berfungsi jika item yang saat ini terpilih memiliki IsSelected = true.
    /// </summary>
    public void ConfirmGunSelectionAndGoBack(int indexToConfirm)
    {
        if (snapToItem == null)
        {
            Debug.LogError("SnapToItem reference is missing. Cannot perform confirmation logic.");
            return;
        }

        // 1. Dapatkan indeks item yang saat ini tersnap.
        // Asumsi: SnapToItem memiliki properti publik untuk currentSnappedIndex.
        // KARENA SnapToItem TIDAK memiliki properti publik untuk index yang tersnap, kita akan 
        // menggunakan gunItems.FindIndex() berdasarkan currentSelectedGun.

        // Perhatian: Ini mengasumsikan item dalam availableGuns berurutan sama dengan item UI.

        int currentSnappedIndex = -1;
        if (currentSelectedGun != null)
        {
            currentSnappedIndex = System.Array.IndexOf(availableGuns, currentSelectedGun);
        }

        // 2. Cek apakah index yang dikonfirmasi SAMA dengan index yang saat ini tersnap.
        if (indexToConfirm != currentSnappedIndex)
        {
            // 3. Jika TIDAK SAMA, picu snap ke index yang ditekan/dikonfirmasi.
            Debug.Log($"Mencoba konfirmasi item {indexToConfirm}, tapi item yang tersnap adalah {currentSnappedIndex}. Melakukan snap terlebih dahulu.");

            // PENTING: Gunakan OnItemClick untuk memicu snap
            snapToItem.OnItemClick(indexToConfirm);

            // TIDAK MELANJUTKAN konfirmasi. Konfirmasi yang sebenarnya akan dipanggil 
            // oleh SelectGunByIndex() di akhir proses snap, atau konfirmasi dipanggil 
            // lagi oleh pengguna setelah snap selesai.
            return;
        }


        // 4. Jika SAMA (atau jika item sudah tersnap, dan proses snap selesai), lanjutkan konfirmasi.

        // Periksa validasi isSelected:
        bool isCurrentItemSelected = gunItems.Count > currentSnappedIndex && gunItems[currentSnappedIndex].IsSelected;

        if (!isCurrentItemSelected)
        {
            Debug.LogWarning("Please select a gun by snapping to it before confirming.");
            // Kasus ini seharusnya tidak terjadi jika logika snapToItem dan SetSelectedIndex sudah benar.
            return;
        }

        // Logika konfirmasi yang ada
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
            // CRITICAL FIX: Save the selection to the persistent manager
            if (GameSelectionManager.Instance != null)
            {
                GameSelectionManager.Instance.SetConfirmedGun(currentSelectedGun);

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
