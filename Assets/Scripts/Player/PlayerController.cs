using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class PlayerController : MonoBehaviour
{
    public float lockRadius = 113f; // Potentially player-related?

    public Transform spawnPoint;
    private Camera cam;
    private Collider planeCollider; // Reference to the "ground" plane for mouse interaction
    private Ray ray;
    private RaycastHit hit;
    private AircraftController controlledAircraft; // Reference to the aircraft being controlled

    public static int totalScore = 0;

    // Start is called before the first frame update
    void Start()
    {
        if (VhcChgr.vehicleToLoad != null)
        {
            // Instantiate the vehicle at the spawn point.
            Instantiate(VhcChgr.vehicleToLoad, spawnPoint.position, spawnPoint.rotation);
            // Optionally, you can set VhcChgr.vehicleToLoad to null after instantiating
            // so that it doesn't get instantiated again if the scene is reloaded.
            VhcChgr.vehicleToLoad = null;
        }
        else
        {
            Debug.LogError("No vehicle to load!");
        }

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
                script.enabled = true;
            }
        }

        cam = Camera.main;
        planeCollider = GameObject.Find("Plane").GetComponent<Collider>(); // Consider a more robust way to find this

        // Find the initial AircraftController in the scene (you might need a more specific way to assign this)
        controlledAircraft = FindObjectOfType<AircraftController>();
        if (controlledAircraft == null)
        {
            Debug.LogError("No AircraftController found in the scene!");
            enabled = false;
        }

        totalScore = 0;
    }

    // Update is called once per frame
    void Update()
    {
        HandleMovementInput();
        HandleWeaponInput();
        // HandleLocking(); // If locking logic is more player-centric
    }
    public void AddScore(int score)
    {
        totalScore += score;
        Debug.Log("Player Score: " + totalScore); // Log the score for testing
        // You would typically update UI here
    }


    void HandleMovementInput()
    {
        if (cam == null || planeCollider == null || controlledAircraft == null) return;

        ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider == planeCollider)
            {
                controlledAircraft.SetTargetPosition(hit.point);
            }
            else
            {
                controlledAircraft.ResetRotation();
            }
        }
        else
        {
            controlledAircraft.ResetRotation();
        }
    }

    void HandleWeaponInput()
    {
        if (controlledAircraft == null)
        {
            Debug.LogError("Couldn't found controlledAircraft");
        }

        if (Input.GetButtonDown("Payload")) // Right Mouse Button for missiles
        {
            controlledAircraft.FirePayload();
        }
        if (Input.GetButtonDown("Change Payload")) // X key to switch payload
        {
            controlledAircraft.SwitchPayload();
        }
    }

    // Example of potential player-centric locking logic
    /*
    void HandleLocking()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, lockRadius);
        // Implement logic to find and target enemies
        // ... and then communicate the target to the AircraftController if needed
        Debug.DrawWireSphere(transform.position, lockRadius, Color.yellow);
    }
    */
}