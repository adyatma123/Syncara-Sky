using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using System.Linq; // Tambahkan ini jika PayloadManager menggunakan Linq

public class PlayerController : MonoBehaviour
{
    public float lockRadius = 113f;

    public Transform spawnPoint;
    private Camera cam;
    private Collider planeCollider;
    private Ray ray;
    private RaycastHit hit;
    private AircraftController controlledAircraft; // Reference to the aircraft being controlled
    private PayloadManager payloadManager;

    public static int totalScore = 0;

    // NEW: Toggles untuk kontrol tutorial
    [Header("Tutorial Control")]
    [SerializeField] private bool enableMovement = true; // NEW
    [SerializeField] private bool enableWeapons = true; // NEW

    // NEW: Properti untuk mendeteksi input selama tutorial
    public bool MovementDetected { get; private set; } = false;
    public bool PrimaryFireDetected { get; private set; } = false;
    public bool PayloadFireDetected { get; private set; } = false;


    // Start is called before the first frame update
    void Start()
    {
        bool vehicleInstantiated = false;
        GameObject newAircraftInstance = null;

        // -----------------------------------------------------------
        // BAGIAN 1A: INSTANSIASI DARI VhcChgr (Prioritas 1)
        // -----------------------------------------------------------
        if (VhcChgr.vehicleToLoad != null)
        {
            // Instantiate the vehicle at the spawn point.
            newAircraftInstance = Instantiate(
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
                Debug.Log("[PlayerController] Successfully linked to spawned AircraftController via VhcChgr.");
                vehicleInstantiated = true;
            }

            // 2. JAMINAN AKTIVASI SKRIP: Ulangi dan aktifkan SEMUA skrip pada instance ini.
            MonoBehaviour[] scripts = newAircraftInstance.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                script.enabled = true;
            }

            // VhcChgr.vehicleToLoad = null; // Clear static reference (DIJAGA TIDAK DI-NULL UNTUK RESET)
        }
        else
        {
            Debug.LogWarning("VhcChgr.vehicleToLoad is null. Attempting to find existing 'Player' tagged object as fallback.");
        }

        // -----------------------------------------------------------
        // BAGIAN 1B: PENCARIAN FALLBACK (Prioritas 2)
        // -----------------------------------------------------------
        if (!vehicleInstantiated)
        {
            GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
            if (existingPlayer != null)
            {
                controlledAircraft = existingPlayer.GetComponent<AircraftController>();
                if (controlledAircraft != null)
                {
                    Debug.Log("[PlayerController] Successfully linked to existing 'Player' tagged AircraftController.");
                    vehicleInstantiated = true;
                }
                else
                {
                    Debug.LogError("Found object with tag 'Player' but it is missing the AircraftController component!");
                }
            }
        }

        // Final check for controlledAircraft outside of the instantiation/search block
        if (controlledAircraft == null)
        {
            Debug.LogError("PlayerController could not find or instantiate any controlled aircraft.");
        }

        if (newAircraftInstance != null)
        {
            payloadManager = newAircraftInstance.GetComponent<PayloadManager>();
            ApplyConfirmedPayloadLoadout();
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
    }

    // Update is called once per frame
    void Update()
    {
        // Hanya jalankan logika kontrol jika sudah siap
        if (controlledAircraft == null || cam == null || planeCollider == null)
        {
            // Tambahkan logika pencarian per frame untuk kasus yang sangat lambat 
            // ATAU objek player dibuat setelah Start()
            if (controlledAircraft == null)
            {
                FindExistingPlayerPerFrame();
            }

            // Jika masih null, keluar dari Update
            if (controlledAircraft == null) return;
        }

        // Jalankan input handler hanya jika diizinkan
        if (enableMovement) HandleMovementInput();
        if (enableWeapons) HandleWeaponInput();

        // Reset deteksi input per frame
        MovementDetected = false;
        PrimaryFireDetected = false;
        PayloadFireDetected = false;
    }

    /// <summary>
    /// Mencari objek "Player" di setiap frame jika controlledAircraft masih null.
    /// Ini berguna untuk kasus di mana objek diinstansiasi dengan delay.
    /// </summary>
    private void FindExistingPlayerPerFrame()
    {
        GameObject existingPlayer = GameObject.FindGameObjectWithTag("Player");
        if (existingPlayer != null)
        {
            controlledAircraft = existingPlayer.GetComponent<AircraftController>();
            if (controlledAircraft != null)
            {
                Debug.Log("[PlayerController] Successfully linked to 'Player' tagged AircraftController (Delayed Find).");
            }
        }
    }

    /// <summary>
    /// NEW: Mengambil payload yang dikonfirmasi dari GameSelectionManager dan menerapkannya.
    /// </summary>
    private void ApplyConfirmedPayloadLoadout()
    {
        if (payloadManager == null || GameSelectionManager.Instance == null)
        {
            Debug.LogWarning("[PlayerController] PayloadManager atau GameSelectionManager tidak ditemukan. Melewati inisialisasi payload.");
            return;
        }

        Payload[] confirmedPayloads = GameSelectionManager.Instance.ConfirmedPayloadSelections;

        if (confirmedPayloads == null || confirmedPayloads.Length == 0)
        {
            Debug.LogWarning("[PlayerController] GameSelectionManager tidak memiliki loadout payload yang dikonfirmasi.");
            return;
        }

        // Terapkan loadout yang dikonfirmasi ke PayloadManager Player
        for (int i = 0; i < confirmedPayloads.Length; i++)
        {
            if (i < payloadManager.payloadSlots.Length)
            {
                // SetPayloadAtSlotIndex akan otomatis memanggil ReinitializeLoadout()
                payloadManager.SetPayloadAtSlotIndex(i, confirmedPayloads[i]);
            }
            else
            {
                Debug.LogWarning($"[PlayerController] Payload slot index {i} melebihi batas slot pada Player.");
                break;
            }
        }

        Debug.Log($"[PlayerController] {confirmedPayloads.Length} payload berhasil diterapkan dari GameSelectionManager.");
    }


    public void AddScore(int score)
    {
        totalScore += score;
        Debug.Log("Player Score: " + totalScore); // Log the score for testing
        // You would typically update UI here
    }

    // NEW: Public setter untuk kontrol gerakan
    public void SetMovementEnabled(bool state)
    {
        enableMovement = state;
    }

    // NEW: Public setter untuk kontrol senjata
    public void SetWeaponsEnabled(bool state)
    {
        enableWeapons = state;
    }


    void HandleMovementInput()
    {
        // Pengecekan pergerakan mouse (misalnya perubahan posisi mouse)
        if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
        {
            MovementDetected = true;

            enableMovement = true;
        }

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

        // Deteksi input payload
        if (Input.GetButtonDown("Payload")) // Right Mouse Button for missiles
        {
            PayloadFireDetected = true; // Tandai deteksi
            controlledAircraft.FirePayload();
        }
        if (Input.GetButtonDown("Change Payload")) // X key to switch payload
        {
            controlledAircraft.SwitchPayload();
        }

        // Deteksi input senjata utama (Gun)
        if (Input.GetButton("Gun")) // Asumsi "Gun" adalah input fire utama
        {
            PrimaryFireDetected = true; // Tandai deteksi
        }
    }
}
