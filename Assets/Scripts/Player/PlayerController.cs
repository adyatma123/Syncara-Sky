using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class PlayerController : MonoBehaviour
{
    public float lockRadius = 113f; // Potentially player-related?

    private Camera cam;
    private Collider planeCollider; // Reference to the "ground" plane for mouse interaction
    private Ray ray;
    private RaycastHit hit;
    private AircraftController controlledAircraft; // Reference to the aircraft being controlled

    public static int totalScore = 0;

    // Start is called before the first frame update
    void Start()
    {
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
        if (controlledAircraft == null) return;

        if (Input.GetMouseButtonDown(1)) // Right Mouse Button for missiles
        {
            controlledAircraft.FireMissile();
        }

        if (Input.GetButtonDown("Rocket")) // Defined in Input Manager
        {
            controlledAircraft.FireRocket();
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