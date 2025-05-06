using UnityEngine;
using UnityEngine.SceneManagement;

public class VhcChgr : MonoBehaviour
{
    [SerializeField] private ScriptableObject[] scriptableObjects;
    [SerializeField] private VhcDis vehicleDisplay;
    [SerializeField] private float speed = 10f;
    [SerializeField] private string nextSceneName;
    private int currentIndex;
    public static GameObject vehicleToLoad;
    private GameObject selectedVehiclePrefab;

    private void Awake()
    {
        vehicleDisplay.VehicleDisplayer((Vehicles)scriptableObjects[0]);
        UpdateSelectedVehicle();
        DisablePlayerScriptsFunction();
    }

    public void ChangeScriptableObject(int _change)
    {
        currentIndex += _change;
        if (currentIndex < 0) currentIndex = scriptableObjects.Length - 1;
        else if (currentIndex > scriptableObjects.Length - 1) currentIndex = 0;

        if (vehicleDisplay != null) vehicleDisplay.VehicleDisplayer((Vehicles)scriptableObjects[currentIndex]);

        UpdateSelectedVehicle();
        DisablePlayerScriptsFunction();
    }

    void FixedUpdate()
    {
        // Rotate the object around its up axis (Y-axis).
        // Time.deltaTime makes the rotation speed independent of the frame rate.
        transform.Rotate(Vector3.up * speed * Time.deltaTime);
        //You can also use
        //transform.Rotate(0, speed * Time.deltaTime, 0);
    }

    private void UpdateSelectedVehicle()
    {
        Vehicles vehicleData = (Vehicles)scriptableObjects[currentIndex];
        selectedVehiclePrefab = vehicleData.vehiclePrefab;
    }

    public void SelectVehicleAndLoadScene()
    {
        // Set the static variable to hold the vehicle to load.
        if (selectedVehiclePrefab != null)
        {
            vehicleToLoad = selectedVehiclePrefab;
            // Load the next scene.
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("No vehicle selected to load!"); //error check
        }
    }

    void DisablePlayerScriptsFunction()
    {
        // Find all GameObjects with the tag "Player".
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");

        // Iterate through each GameObject found.
        foreach (GameObject playerObject in playerObjects)
        {
            // Get all MonoBehaviour components (which includes scripts) on the current GameObject.
            MonoBehaviour[] scripts = playerObject.GetComponents<MonoBehaviour>();

            // Iterate through each script component.
            foreach (MonoBehaviour script in scripts)
            {
                // Disable the script.
                script.enabled = false;
            }
        }
    }
}
