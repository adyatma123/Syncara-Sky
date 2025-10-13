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

    // NEW: Transform untuk Camera Pivot/Target (tempat rotasi kamera akan berpusat)
    [Tooltip("The new Transform that the camera will follow and rotate around.")]
    [SerializeField] private Transform rotationTargetTransform;

    // NEW: Fields untuk mengontrol offset kamera
    [Tooltip("The horizontal offset (X-axis) for the camera from the Rotation Target (Local Right).")]
    [SerializeField] private float cameraXOffset = 0f;
    [Tooltip("The vertical offset (Y-axis) for the camera from the Rotation Target (Local Up).")]
    [SerializeField] private float cameraYOffset = 0f;
    [Tooltip("The Z-axis distance (depth) for the camera from the Rotation Target (Local Forward).")]
    [SerializeField] private float cameraZDistance = 5f; // New distance field


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
    [SerializeField] private float cameraRotationSpeed = 10f; // Renamed for clarity

    // Diubah: Sekarang menyimpan rotasi awal RotationTargetTransform
    private Quaternion initialRotationTargetRotation;

    private void Awake()
    {
        // Store initial rotation of the target (where the camera rotates around)
        if (rotationTargetTransform != null)
        {
            initialRotationTargetRotation = rotationTargetTransform.rotation;
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
        // 1. Determine the Camera Target Rotation
        Quaternion targetRot;

        // Ensure we have the initial position transform to prevent errors
        if (initialCameraPositionTransform == null) return;

        // Tentukan Transform PoV target berdasarkan state
        Transform targetTransform;
        switch (currentMenuState)
        {
            case MenuState.VehicleSelection:
                targetTransform = initialCameraPositionTransform;
                break;
            case MenuState.LoadoutMenu:
                targetTransform = loadoutCameraPosition;
                break;
            case MenuState.GunLoadout:
                targetTransform = gunCameraPosition;
                break;
            case MenuState.PayloadLoadout:
                targetTransform = payloadCameraPosition;
                break;
            default:
                targetTransform = initialCameraPositionTransform;
                break;
        }

        // Rotasi target awal sama dengan PoV Transform
        targetRot = targetTransform.rotation;

        Vector3 targetPos;

        // JIKA rotationTargetTransform disetel, periksa state untuk menentukan logika posisi
        if (rotationTargetTransform != null && currentMenuState == MenuState.VehicleSelection)
        {
            // LOGIKA ORBIT & OFFSET (HANYA BERLAKU UNTUK VehicleSelection)

            // Dapatkan rotasi PIVOT saat ini
            Quaternion currentPivotRotation = rotationTargetTransform.rotation;

            // Tentukan offset kamera lokal relatif terhadap pivot
            Vector3 localOffset = new Vector3(cameraXOffset, cameraYOffset, -cameraZDistance);

            // Konversi offset lokal ke posisi dunia relatif terhadap pivot yang berputar
            targetPos = rotationTargetTransform.position + (currentPivotRotation * localOffset);

            // Kamera harus selalu menghadap pivot saat mengorbit
            Vector3 lookDirection = rotationTargetTransform.position - menuCamera.transform.position;
            targetRot = Quaternion.LookRotation(lookDirection);
        }
        else
        {
            // LOGIKA POIN PoV TETAP (UNTUK LoadoutMenu, GunLoadout, PayloadLoadout, ATAU JIKA rotationTargetTransform NULL)
            targetPos = targetTransform.position;
            // targetRot sudah diatur ke targetTransform.rotation di awal FixedUpdate()
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

        // 3. Handle Rotation
        if (rotationTargetTransform != null)
        {
            if (currentMenuState == MenuState.VehicleSelection)
            {
                // BARU: Putar continuous rotationTargetTransform (pivot kamera)
                rotationTargetTransform.Rotate(Vector3.up * cameraRotationSpeed * Time.deltaTime);

                // JIKA ada vehicleToRotate, pastikan ia diam pada rotasi awal
                if (vehicleToRotate != null)
                {
                    vehicleToRotate.rotation = initialRotationTargetRotation;
                }
            }
            else
            {
                // BARU: Kembalikan rotasi target secara mulus ke rotasi awal
                rotationTargetTransform.rotation = Quaternion.Slerp(
                    rotationTargetTransform.rotation,
                    initialRotationTargetRotation,
                    Time.deltaTime * cameraRotationSpeed
                );
            }
        }

        // PASTIKAN VehicleToRotate diam pada rotasi awalnya (jika RotationTargetTransform diatur)
        if (vehicleToRotate != null && rotationTargetTransform != null)
        {
            // Pastikan vehicleToRotate tidak berputar sendiri (karena rotationTargetTransform yang berputar)
            vehicleToRotate.rotation = initialRotationTargetRotation;
        }
        else if (vehicleToRotate != null && rotationTargetTransform == null)
        {
            // Fallback lama: Jika RotationTargetTransform tidak diatur, pertahankan logika rotasi kendaraan lama
            if (currentMenuState == MenuState.VehicleSelection)
            {
                vehicleToRotate.Rotate(Vector3.up * cameraRotationSpeed * Time.deltaTime);
            }
            else
            {
                vehicleToRotate.rotation = Quaternion.Slerp(
                    vehicleToRotate.rotation,
                    initialRotationTargetRotation,
                    Time.deltaTime * cameraRotationSpeed
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
        currentMenuState = MenuState.LoadoutMenu; // Transition state: Rotation stops/resets
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
