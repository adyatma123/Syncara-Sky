// VhcChgr.cs (Revised for Payload Slot Transfer and MenuController Accessibility)
using UnityEngine;
using UnityEngine.SceneManagement;

public class VhcChgr : MonoBehaviour
{
    [Header("Menu Components")]
    [Tooltip("Reference to the script handling UI/Camera transitions.")]
    [SerializeField] private MenuCameraController menuController;

    // NEW: Public property for accessibility (Fixes 'inaccessible' error)
    public MenuCameraController MenuController => menuController;

    [Header("Vehicle Selection")]
    [SerializeField] private ScriptableObject[] scriptableObjects;
    [SerializeField] private VhcDis vehicleDisplay;
    [SerializeField] private string nextSceneName;
    [SerializeField] private StageData nextStageData;


    private int currentIndex;
    public static GameObject vehicleToLoad;
    private GameObject selectedVehiclePrefab;
    public static Guns selectedGunData;

    private void Start()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayMusic("Hangar BGM");
        }
    }

    private void Awake()
    {
        if (menuController == null)
        {
            Debug.LogError("Menu Controller not assigned to VhcChgr. UI transitions will fail.", this);
            enabled = false;
            return;
        }

        // Assuming Vehicles is a type derived from ScriptableObject
        if (scriptableObjects.Length > 0 && scriptableObjects[0] is Vehicles)
        {
            vehicleDisplay.VehicleDisplayer((Vehicles)scriptableObjects[0]);
            UpdateSelectedVehicle();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GoBackInMenu();
        }
    }

    public void ChangeScriptableObject(int _change)
    {
        currentIndex += _change;
        if (currentIndex < 0) currentIndex = scriptableObjects.Length - 1;
        else if (currentIndex > scriptableObjects.Length - 1) currentIndex = 0;

        if (vehicleDisplay != null && scriptableObjects[currentIndex] is Vehicles)
            vehicleDisplay.VehicleDisplayer((Vehicles)scriptableObjects[currentIndex]);

        UpdateSelectedVehicle();
    }

    private void UpdateSelectedVehicle()
    {
        if (scriptableObjects[currentIndex] is Vehicles vehicleData)
        {
            selectedVehiclePrefab = vehicleData.vehiclePrefab;
        }
    }

    /// <summary>
    /// Called when the player selects a vehicle. Transitions to the main Loadout Menu.
    /// </summary>
    public void SelectVehicle()
    {
        if (selectedVehiclePrefab != null)
        {
            vehicleToLoad = selectedVehiclePrefab;

            // NEW LOGIC: Dapatkan jumlah slot dari PayloadManager
            int slotCount = GetPayloadSlotCountFromSelectedVehicle(selectedVehiclePrefab);

            // Kirim jumlah slot ke GameSelectionManager sebelum transisi
            if (GameSelectionManager.Instance != null)
            {
                GameSelectionManager.Instance.SetVehiclePayloadSlotCount(slotCount);

                // 🔽 Tambahkan baris ini untuk memaksa PayloadSelector refresh
                PayloadSelector payloadSelector = FindObjectOfType<PayloadSelector>(true);
                if (payloadSelector != null)
                {
                    payloadSelector.InitializeSlotButtons();
                    Debug.Log($"PayloadSelector refreshed with {slotCount} slots.");
                }
                else
                {
                    Debug.LogWarning("PayloadSelector not found in current scene (might be inactive or in next menu).");
                }
            }

            // Transisi ke menu berikutnya
            menuController.TransitionToLoadout();
            SoundManager.Instance.PlaySFX("Click");
        }
        else
        {
            Debug.LogWarning("No vehicle selected to load!");
        }
    }

    /// <summary>
    /// Helper: Mengambil jumlah slot dari PayloadManager yang ada pada prefab kendaraan.
    /// </summary>
    private int GetPayloadSlotCountFromSelectedVehicle(GameObject vehiclePrefab)
    {
        // ... (Logika GetPayloadSlotCountFromSelectedVehicle tetap sama) ...
        PayloadManager payloadMgr = vehiclePrefab.GetComponent<PayloadManager>();

        if (payloadMgr != null && payloadMgr.payloadSlots != null)
        {
            return payloadMgr.payloadSlots.Length;
        }

        Debug.LogWarning($"PayloadManager not found or payloadSlots array is null on prefab {vehiclePrefab.name}. Defaulting to 4 slots.");
        return 4;
    }

    /// <summary>
    /// PUBLIC ACCESS POINT: This function is called by the 'Back' button or the ESC key.
    /// It delegates the intelligent back-navigation to the MenuCameraController.
    /// </summary>
    public void GoBackInMenu()
    {
        // NEW: Gunakan properti publik untuk akses
        if (MenuController != null)
        {
            MenuController.GoBack();
        }
    }

    public void BackToVhcSlct()
    {
        GoBackInMenu();
        SoundManager.Instance.PlaySFX("Click");
    }

    public void GunMenu()
    {
        if (MenuController != null) MenuController.TransitionToGunMenu();
        SoundManager.Instance.PlaySFX("Click");
    }

    public void PayloadMenu()
    {
        // NEW: Memanggil transisi ke Slot Selection Panel
        if (MenuController != null) MenuController.TransitionToPayloadMenu();
        SoundManager.Instance.PlaySFX("Click");
    }

    public void LoadScene()
    {
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadNewScene(nextSceneName, nextStageData);
        }
        else
        {
            Debug.LogError("SceneLoader.Instance not found! Falling back to direct load (without loading screen).");
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
