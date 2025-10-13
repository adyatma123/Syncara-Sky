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
        DisablePlayerScriptsFunction();
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
        DisablePlayerScriptsFunction();
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
    }

    public void GunMenu()
    {
        menuController.TransitionToGunMenu();
    }

    public void PayloadMenu()
    {
        menuController.TransitionToPayloadMenu();
    }

    public void LoadScene()
    {
        if (selectedVehiclePrefab != null)
        {
            SceneManager.LoadScene(nextSceneName);

        }
        else
        {
            Debug.LogWarning("No scene selected to load!");
        }
    }

    void DisablePlayerScriptsFunction()
    {
        // Existing cleanup logic
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject playerObject in playerObjects)
        {
            MonoBehaviour[] scripts = playerObject.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                script.enabled = false;
            }
        }
    }
}
