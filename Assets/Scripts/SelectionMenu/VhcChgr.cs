// VhcChgr.cs (Revised to expose Current Vehicle data)
using UnityEngine;
using UnityEngine.SceneManagement;

public class VhcChgr : MonoBehaviour
{
    [Header("Menu Components")]
    [Tooltip("Reference to the script handling UI/Camera transitions.")]
    [SerializeField] private MenuCameraController menuController;

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
    public Vehicles CurrentVehicle { get; private set; }

    // 🌟 BARU: Properti publik untuk mendapatkan data kendaraan yang saat ini dipilih
    private void UpdateSelectedVehicle()
    {
        if (scriptableObjects[currentIndex] is Vehicles vehicleData)
        {
            CurrentVehicle = vehicleData;
            selectedVehiclePrefab = vehicleData.vehiclePrefab;

            Debug.Log($"[VhcChgr] Current Vehicle Updated: {vehicleData.name} Tier {vehicleData.Tier}");
        }
    }

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

        if (currentIndex < 0)
            currentIndex = scriptableObjects.Length - 1;
        else if (currentIndex > scriptableObjects.Length - 1)
            currentIndex = 0;

        // UPDATE DATA FIRST
        UpdateSelectedVehicle();

        // THEN update visual
        if (vehicleDisplay != null && scriptableObjects[currentIndex] is Vehicles)
        {
            vehicleDisplay.VehicleDisplayer((Vehicles)scriptableObjects[currentIndex]);
        }

        // THEN refresh selectors
        GunSelector gunSelector = FindObjectOfType<GunSelector>(true);
        if (gunSelector != null && gunSelector.gameObject.activeInHierarchy)
        {
            gunSelector.InitializeGunSelector();
        }

        PayloadSelector payloadSelector = FindObjectOfType<PayloadSelector>(true);
        if (payloadSelector != null && payloadSelector.gameObject.activeInHierarchy)
        {
            payloadSelector.InitializePayloadItems();
        }
    }

    public void SelectVehicle()
    {
        if (selectedVehiclePrefab != null)
        {
            vehicleToLoad = selectedVehiclePrefab;

            int slotCount = GetPayloadSlotCountFromSelectedVehicle(selectedVehiclePrefab);

            if (GameSelectionManager.Instance != null)
            {
                GameSelectionManager.Instance.SetVehiclePayloadSlotCount(slotCount);

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

            menuController.TransitionToLoadout();
            SoundManager.Instance.PlaySFX("Click");
        }
        else
        {
            Debug.LogWarning("No vehicle selected to load!");
        }
    }

    private int GetPayloadSlotCountFromSelectedVehicle(GameObject vehiclePrefab)
    {
        PayloadManager payloadMgr = vehiclePrefab.GetComponent<PayloadManager>();

        if (payloadMgr != null && payloadMgr.payloadSlots != null)
        {
            return payloadMgr.payloadSlots.Length;
        }

        Debug.LogWarning($"PayloadManager not found or payloadSlots array is null on prefab {vehiclePrefab.name}. Defaulting to 4 slots.");
        return 4;
    }

    public void GoBackInMenu()
    {
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