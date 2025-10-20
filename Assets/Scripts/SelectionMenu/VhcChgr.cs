using UnityEngine;
using UnityEngine.SceneManagement;

public class VhcChgr : MonoBehaviour
{
    [Header("Menu Components")]
    [Tooltip("Reference to the script handling UI/Camera transitions.")]
    [SerializeField] private MenuCameraController menuController;

    [Header("Vehicle Selection")]
    [SerializeField] private ScriptableObject[] scriptableObjects;
    [SerializeField] private VhcDis vehicleDisplay;
    [SerializeField] private string nextSceneName;

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
        // Ensure MenuController is available
        if (menuController == null)
        {
            Debug.LogError("Menu Controller not assigned to VhcChgr. UI transitions will fail.", this);
            enabled = false;
            return;
        }

        vehicleDisplay.VehicleDisplayer((Vehicles)scriptableObjects[0]);
        UpdateSelectedVehicle();
    }

    void Update()
    {
        // Check for the Escape key press to navigate back
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Call the shared 'Back' function on the menu controller
            GoBackInMenu();
        }
    }

    public void ChangeScriptableObject(int _change)
    {
        // Note: We rely on MenuCameraController to enforce the state check before allowing the vehicle change.
        currentIndex += _change;
        if (currentIndex < 0) currentIndex = scriptableObjects.Length - 1;
        else if (currentIndex > scriptableObjects.Length - 1) currentIndex = 0;

        if (vehicleDisplay != null) vehicleDisplay.VehicleDisplayer((Vehicles)scriptableObjects[currentIndex]);

        UpdateSelectedVehicle();
    }

    private void UpdateSelectedVehicle()
    {
        Vehicles vehicleData = (Vehicles)scriptableObjects[currentIndex];
        selectedVehiclePrefab = vehicleData.vehiclePrefab;
    }

    /// <summary>
    /// Called when the player selects a vehicle. Transitions to the main Loadout Menu.
    /// </summary>
    public void SelectVehicle()
    {
        if (selectedVehiclePrefab != null)
        {
            vehicleToLoad = selectedVehiclePrefab;
            menuController.TransitionToLoadout(); // Delegate UI/Camera transition
            SoundManager.Instance.PlaySFX("Click");
        }
        else
        {
            Debug.LogWarning("No vehicle selected to load!");
        }
    }

    /// <summary>
    /// PUBLIC ACCESS POINT: This function is called by the 'Back' button or the ESC key.
    /// It delegates the intelligent back-navigation to the MenuCameraController.
    /// </summary>
    public void GoBackInMenu()
    {
        menuController.GoBack(); // Delegate multi-level back logic
    }

    // This is now redundant, replaced by GoBackInMenu, but keeping public access name for clarity
    public void BackToVhcSlct()
    {
        GoBackInMenu();

        SoundManager.Instance.PlaySFX("Click");
    }

    public void GunMenu()
    {
        menuController.TransitionToGunMenu();

        SoundManager.Instance.PlaySFX("Click");
    }

    public void PayloadMenu()
    {
        menuController.TransitionToPayloadMenu();

        SoundManager.Instance.PlaySFX("Click");
    }

    public void LoadScene()
    {
        if (SceneLoader.Instance != null)
        {
            // *** This is the key change: delegating the load process ***
            SceneLoader.Instance.LoadNewScene(nextSceneName);
        }
        else
        {
            // Fallback for debugging/error handling
            Debug.LogError("SceneLoader.Instance not found! Falling back to direct load (without loading screen).");
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
