using UnityEngine;

/// <summary>
/// Manages the state, UI visibility, camera transitions, and visual rotation 
/// for the vehicle selection and loadout menus.
/// </summary>
public class MenuCameraController : MonoBehaviour
{
    // Enum to define the current camera/UI state
    private enum MenuState { VehicleSelection, LoadoutMenu, GunLoadout, PayloadLoadout }
    [Header("Current State")]
    [Tooltip("The current state of the menu.")]
    [SerializeField] private MenuState currentMenuState = MenuState.VehicleSelection;

    [Header("UI Panels")]
    [Tooltip("The panel to hide after selecting the vehicle.")]
    [SerializeField] private GameObject vehicleSelection;
    [Tooltip("The main Loadout Menu panel (shows Gun/Payload buttons).")]
    [SerializeField] private GameObject loadoutMenu;
    [Tooltip("The specific UI panel for Gun configuration.")]
    [SerializeField] private GameObject gunConfigPanel;
    [Tooltip("The specific UI panel for Payload configuration.")]
    [SerializeField] private GameObject payloadConfigPanel;


    [Header("Camera & Rotation")]
    [Tooltip("The Camera to move and control (usually the Main Camera).")]
    [SerializeField] private Camera menuCamera;
    [Tooltip("The Transform of the rotating vehicle object.")]
    [SerializeField] private Transform vehicleToRotate;

    // Camera PoV Transforms
    [Tooltip("The Transform for the Initial PoV (Vehicle Selection).")]
    [SerializeField] private Transform initialCameraPositionTransform;
    [Tooltip("The Transform for the General Loadout view (main transition).")]
    [SerializeField] private Transform loadoutCameraPosition;
    [Tooltip("The Transform for the Gun Loadout view (Gun PoV).")]
    [SerializeField] private Transform gunCameraPosition;
    [Tooltip("The Transform for the Payload Loadout view (Payload PoV).")]
    [SerializeField] private Transform payloadCameraPosition;

    [Tooltip("The speed at which the camera moves to the new position.")]
    [SerializeField] private float cameraMoveSpeed = 5f;
    [Tooltip("The continuous rotation speed (used in VehicleSelection state).")]
    [SerializeField] private float vehicleRotationSpeed = 10f;


    private Quaternion initialVehicleRotation;

    private void Awake()
    {
        // Store initial vehicle rotation (usually Quaternion.identity if placed straight)
        if (vehicleToRotate != null)
        {
            initialVehicleRotation = vehicleToRotate.rotation;
        }

        // --- SNAP CAMERA TO INITIAL POSITION AND SET INITIAL UI STATE ---
        if (menuCamera != null && initialCameraPositionTransform != null)
        {
            menuCamera.transform.position = initialCameraPositionTransform.position;
            menuCamera.transform.rotation = initialCameraPositionTransform.rotation;
        }

        if (vehicleSelection != null) vehicleSelection.SetActive(true);
        if (loadoutMenu != null) loadoutMenu.SetActive(false);
        if (gunConfigPanel != null) gunConfigPanel.SetActive(false);
        if (payloadConfigPanel != null) payloadConfigPanel.SetActive(false);
    }

    void FixedUpdate()
    {
        // 1. Determine the Camera Target (Position and Rotation)
        Vector3 targetPos;
        Quaternion targetRot;

        // Ensure we have the initial position transform to prevent errors
        if (initialCameraPositionTransform == null) return;

        switch (currentMenuState)
        {
            case MenuState.VehicleSelection:
                targetPos = initialCameraPositionTransform.position;
                targetRot = initialCameraPositionTransform.rotation;
                break;
            case MenuState.LoadoutMenu:
                targetPos = loadoutCameraPosition.position;
                targetRot = loadoutCameraPosition.rotation;
                break;
            case MenuState.GunLoadout:
                targetPos = gunCameraPosition.position;
                targetRot = gunCameraPosition.rotation;
                break;
            case MenuState.PayloadLoadout:
                targetPos = payloadCameraPosition.position;
                targetRot = payloadCameraPosition.rotation;
                break;
            default:
                targetPos = initialCameraPositionTransform.position;
                targetRot = initialCameraPositionTransform.rotation;
                break;
        }

        // 2. Smoothly move the camera to the target position
        if (menuCamera != null)
        {
            menuCamera.transform.position = Vector3.Lerp(
                menuCamera.transform.position,
                targetPos,
                Time.deltaTime * cameraMoveSpeed
            );

            menuCamera.transform.rotation = Quaternion.Slerp(
                menuCamera.transform.rotation,
                targetRot,
                Time.deltaTime * cameraMoveSpeed
            );
        }

        // 3. Handle Vehicle Rotation
        if (vehicleToRotate != null)
        {
            if (currentMenuState == MenuState.VehicleSelection)
            {
                // Rotate continuously only in the initial selection menu
                vehicleToRotate.Rotate(Vector3.up * vehicleRotationSpeed * Time.deltaTime);
            }
            else
            {
                // Smoothly rotate the vehicle back to its initial rotation (0, 0, 0) in all other menus
                vehicleToRotate.rotation = Quaternion.Slerp(
                    vehicleToRotate.rotation,
                    initialVehicleRotation,
                    Time.deltaTime * vehicleRotationSpeed
                );
            }
        }
    }

    // --- Public Functions for VhcChgr to call ---

    /// <summary>
    /// Implements the multi-level "Back" functionality triggered by the ESC key or Back button.
    /// </summary>
    public void GoBack()
    {
        switch (currentMenuState)
        {
            case MenuState.PayloadLoadout:
                // From Payload Config -> Go back to main Loadout Menu
                TransitionToLoadoutMenuFromConfig();
                break;
            case MenuState.GunLoadout:
                // From Gun Config -> Go back to main Loadout Menu
                TransitionToLoadoutMenuFromConfig();
                break;
            case MenuState.LoadoutMenu:
                // From main Loadout Menu -> Go back to Vehicle Selection
                TransitionToSelection();
                break;
            case MenuState.VehicleSelection:
                // Already at the main menu, ignore or handle quitting the game
                Debug.Log("Exiting application or opening pause menu.");
                // Application.Quit(); // Uncomment this line if ESC should quit from main menu
                break;
        }
    }

    // --- Transition Methods ---

    public void TransitionToLoadout()
    {
        currentMenuState = MenuState.LoadoutMenu; // Transition state: Vehicle rotation stops/resets
        // UI Logic: Hide Selection, Show Loadout Main
        if (vehicleSelection != null) vehicleSelection.SetActive(false);
        if (loadoutMenu != null) loadoutMenu.SetActive(true);
    }

    public void TransitionToGunMenu()
    {
        currentMenuState = MenuState.GunLoadout;
        // UI Logic: Hide Loadout Main, Show Gun Config
        if (loadoutMenu != null) loadoutMenu.SetActive(false);
        if (gunConfigPanel != null) gunConfigPanel.SetActive(true);
    }

    public void TransitionToPayloadMenu()
    {
        currentMenuState = MenuState.PayloadLoadout;
        // UI Logic: Hide Loadout Main, Show Payload Config
        if (loadoutMenu != null) loadoutMenu.SetActive(false);
        if (payloadConfigPanel != null) payloadConfigPanel.SetActive(true);
    }

    /// <summary>
    /// Handles the transition from a config panel (Gun/Payload) back to the main Loadout Menu.
    /// </summary>
    private void TransitionToLoadoutMenuFromConfig()
    {
        currentMenuState = MenuState.LoadoutMenu;
        // UI Logic: Hide Config Panels, Show Loadout Main
        if (gunConfigPanel != null) gunConfigPanel.SetActive(false);
        if (payloadConfigPanel != null) payloadConfigPanel.SetActive(false);
        if (loadoutMenu != null) loadoutMenu.SetActive(true);
    }

    public void TransitionToSelection()
    {
        // Go back to the main selection view (Initial PoV, rotation ON)
        currentMenuState = MenuState.VehicleSelection;
        // UI Logic: Hide ALL, Show Vehicle Selection
        if (vehicleSelection != null) vehicleSelection.SetActive(true);
        if (loadoutMenu != null) loadoutMenu.SetActive(false);
        if (gunConfigPanel != null) gunConfigPanel.SetActive(false);
        if (payloadConfigPanel != null) payloadConfigPanel.SetActive(false);
    }
}
