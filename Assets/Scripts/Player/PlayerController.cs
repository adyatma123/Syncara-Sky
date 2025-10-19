using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class PlayerController : MonoBehaviour
{
    public float lockRadius = 113f;

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
        // -----------------------------------------------------------
        // BAGIAN 1: INSTANSIASI DAN AKTIVASI SKRIP PESAWAT
        // -----------------------------------------------------------
        if (VhcChgr.vehicleToLoad != null)
        {
            // Instantiate the vehicle at the spawn point.
            GameObject newAircraftInstance = Instantiate(
                VhcChgr.vehicleToLoad,
                spawnPoint.position,
                spawnPoint.rotation
            );

            // PENTING: Jamin objek memiliki tag "Player"
            if (!newAircraftInstance.CompareTag("Player"))
            {
                newAircraftInstance.tag = "Player";
                Debug.Log("[PlayerController] Tag 'Player' added to instantiated aircraft.");
            }

            // 1. Dapatkan referensi AircraftController
            controlledAircraft = newAircraftInstance.GetComponent<AircraftController>();
            if (controlledAircraft == null)
            {
                Debug.LogError("The instantiated vehicle is missing the AircraftController component!");
            }
            else
            {
                Debug.Log("[PlayerController] Successfully linked to spawned AircraftController.");
            }

            // 2. JAMINAN AKTIVASI SKRIP: Ulangi dan aktifkan SEMUA skrip pada instance ini.
            MonoBehaviour[] scripts = newAircraftInstance.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                script.enabled = true;
            }

            VhcChgr.vehicleToLoad = null; // Clear static reference
        }
        else
        {
            // Fallback jika tidak ada kendaraan yang dipilih
            Debug.LogError("No vehicle to load! Check VhcChgr setup or if a vehicle was selected.");
        }

        // -----------------------------------------------------------
        // BAGIAN 2: PENGATURAN REFERENSI LAIN
        // -----------------------------------------------------------
        cam = Camera.main;
        // Mencari Plane di Start() lebih aman daripada di Update()
        GameObject planeObj = GameObject.Find("Plane");
        if (planeObj != null)
        {
            planeCollider = planeObj.GetComponent<Collider>();
        }

        if (cam == null) Debug.LogError("Main Camera not found!");
        if (planeCollider == null) Debug.LogError("'Plane' object or its Collider not found!");

        totalScore = 0;

        // CATATAN: Blok kode yang lama untuk menonaktifkan/mengaktifkan semua objek "Player" 
        // secara global telah DIHAPUS karena rentan terhadap masalah waktu.
    }

    // Update is called once per frame
    void Update()
    {
        // Hanya jalankan logika kontrol jika sudah siap
        if (controlledAircraft == null || cam == null || planeCollider == null)
        {
            return;
        }

        HandleMovementInput();
        HandleWeaponInput();
    }

    public void AddScore(int score)
    {
        totalScore += score;
        Debug.Log("Player Score: " + totalScore); // Log the score for testing
        // You would typically update UI here
    }


    void HandleMovementInput()
    {
        ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider == planeCollider)
            {
                controlledAircraft.SetTargetPosition(hit.point);
            }
            else
            {
                // Jika raycast mengenai objek lain, reset rotasi atau abaikan input
                controlledAircraft.ResetRotation();
            }
        }
        else
        {
            // Jika raycast tidak mengenai apa-apa, reset rotasi
            controlledAircraft.ResetRotation();
        }
    }

    void HandleWeaponInput()
    {
        // controlledAircraft dijamin tidak null karena ada pengecekan di Update()

        if (Input.GetButtonDown("Payload")) // Right Mouse Button for missiles
        {
            controlledAircraft.FirePayload();
        }
        if (Input.GetButtonDown("Change Payload")) // X key to switch payload
        {
            controlledAircraft.SwitchPayload();
        }
    }
}
