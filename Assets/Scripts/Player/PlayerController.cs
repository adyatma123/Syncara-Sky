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
            // NOTE: The instantiated vehicle MUST have the "Player" tag for the search logic below to work.
            Instantiate(VhcChgr.vehicleToLoad, spawnPoint.position, spawnPoint.rotation);
            // Optionally, you can set VhcChgr.vehicleToLoad to null after instantiating
            // so that it doesn't get instantiated again if the scene is reloaded.
            VhcChgr.vehicleToLoad = null;
        }
        else
        {
            Debug.LogError("No vehicle to load!");
        }

        // The following block of code disables all scripts on all "Player" objects found at start.
        // If your new aircraft is being spawned, this block should either be removed or moved to the
        // moment the aircraft is spawned. For safety, I'm leaving it as-is but noting its potential conflict.
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");

        // Iterate through each GameObject found.
        foreach (GameObject playerObject in playerObjects)
        {
            // Get all MonoBehaviour components (which includes scripts) on the current GameObject.
            MonoBehaviour[] scripts = playerObject.GetComponents<MonoBehaviour>();

            // Iterate through each script component.
            foreach (MonoBehaviour script in scripts)
            {
                // Disable the script. (Scripts were originally set to enabled = true)
                script.enabled = true;
            }
        }

        // REMOVED: cam = Camera.main;
        // REMOVED: planeCollider = GameObject.Find("Plane").GetComponent<Collider>(); 

        // REMOVED: controlledAircraft = FindObjectOfType<AircraftController>();
        // The aircraft hasn't finished spawning here, so we will find it in Update().

        totalScore = 0;
    }

    // Update is called once per frame
    void Update()
    {
        // NEW: DYNAMIC ESSENTIAL REFERENCES FINDER (Aircraft, Camera, Plane)
        bool ready = true;

        if (controlledAircraft == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                controlledAircraft = playerObj.GetComponent<AircraftController>();
                if (controlledAircraft != null)
                {
                    Debug.Log("[PlayerController] Successfully linked to spawned AircraftController.");
                }
            }
            if (controlledAircraft == null) ready = false;
        }

        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) ready = false;
        }

        if (planeCollider == null)
        {
            GameObject planeObj = GameObject.Find("Plane");
            if (planeObj != null)
            {
                planeCollider = planeObj.GetComponent<Collider>();
            }
            if (planeCollider == null) ready = false;
        }

        // If any essential component is missing, return early.
        if (!ready) return;

        // Old comment removed: // If controlledAircraft is still null, we cannot proceed with control logic yet.
        // Old redundant check removed: // if (controlledAircraft == null) return;


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
        // This check is now mostly redundant as the start of Update() handles it, 
        // but checking controlledAircraft just for safety.
        // Removed unnecessary cam and planeCollider null checks since they are checked in Update()
        if (controlledAircraft == null) return;

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
        // controlledAircraft is guaranteed not to be null if the rest of Update() runs.
        if (controlledAircraft == null)
        {
            // This error should no longer happen once the aircraft is found.
            Debug.LogError("Couldn't found controlledAircraft in HandleWeaponInput");
            return;
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
