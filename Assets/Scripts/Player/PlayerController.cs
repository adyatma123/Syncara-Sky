using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    public float lockRadius = 113f;

    public Transform spawnPoint;
    private Camera cam;
    private Collider planeCollider;
    private Ray ray;
    private RaycastHit hit;
    private AircraftController controlledAircraft;
    private PayloadManager payloadManager;

    public static int totalScore = 0;

    [Header("Tutorial Control")]
    [SerializeField] private bool enableMovement = true;
    [SerializeField] private bool enableWeapons = true;

    public bool MovementDetected { get; private set; } = false;
    public bool PrimaryFireDetected { get; private set; } = false;
    public bool PayloadFireDetected { get; private set; } = false;

    [Header("Viewport Boundary Settings")]
    [Tooltip("Batas minimum area pandang (0 = paling kiri/bawah layar). Beri sedikit padding agar tidak terlalu mepet ke ujung.")]
    [SerializeField][Range(0f, 0.5f)] private float minViewportPadding = 0.02f;
    [Tooltip("Batas maksimum area pandang (1 = paling kanan/atas layar).")]
    [SerializeField][Range(0.5f, 1f)] private float maxViewportPadding = 0.98f;


    void Start()
    {
        bool vehicleInstantiated = false;
        GameObject newAircraftInstance = null;

        if (VhcChgr.vehicleToLoad != null)
        {
            newAircraftInstance = Instantiate(
                VhcChgr.vehicleToLoad,
                spawnPoint.position,
                spawnPoint.rotation
            );

            if (!newAircraftInstance.CompareTag("Player"))
            {
                newAircraftInstance.tag = "Player";
                Debug.Log("[PlayerController] Tag 'Player' added to instantiated aircraft.");
            }

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

            MonoBehaviour[] scripts = newAircraftInstance.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                script.enabled = true;
            }
        }
        else
        {
            Debug.LogWarning("VhcChgr.vehicleToLoad is null. Attempting to find existing 'Player' tagged object as fallback.");
        }

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

        if (controlledAircraft == null)
        {
            Debug.LogError("PlayerController could not find or instantiate any controlled aircraft.");
        }

        if (newAircraftInstance != null)
        {
            payloadManager = newAircraftInstance.GetComponent<PayloadManager>();
            ApplyConfirmedPayloadLoadout();
        }

        cam = Camera.main;
        GameObject planeObj = GameObject.Find("Plane");
        if (planeObj != null)
        {
            planeCollider = planeObj.GetComponent<Collider>();
        }

        if (cam == null) Debug.LogError("Main Camera not found!");
        if (planeCollider == null) Debug.LogError("'Plane' object or its Collider not found!");

        totalScore = 0;
    }

    void Update()
    {
        if (controlledAircraft == null || cam == null || planeCollider == null)
        {
            if (controlledAircraft == null)
            {
                FindExistingPlayerPerFrame();
            }

            if (controlledAircraft == null) return;
        }

        if (enableMovement) HandleMovementInput();
        if (enableWeapons) HandleWeaponInput();

        MovementDetected = false;
        PrimaryFireDetected = false;
        PayloadFireDetected = false;
    }

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

        for (int i = 0; i < confirmedPayloads.Length; i++)
        {
            if (i < payloadManager.payloadSlots.Length)
            {
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
        Debug.Log("Player Score: " + totalScore);
    }

    public void SetMovementEnabled(bool state)
    {
        enableMovement = state;
    }

    public void SetWeaponsEnabled(bool state)
    {
        enableWeapons = state;
    }

    void HandleMovementInput()
    {
        if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
        {
            MovementDetected = true;
            enableMovement = true;
        }

        // 🌟 BARU: Logika Penguncian Posisi di dalam Viewport Kamera
        // 1. Ubah posisi mouse pixel (Screen Space) menjadi koordinat persentase Viewport (0.0 sampai 1.0)
        Vector3 mouseViewportPos = cam.ScreenToViewportPoint(Input.mousePosition);

        // 2. Kunci (Clamp) nilai X dan Y agar tidak pernah keluar dari batas yang ditentukan
        mouseViewportPos.x = Mathf.Clamp(mouseViewportPos.x, minViewportPadding, maxViewportPadding);
        mouseViewportPos.y = Mathf.Clamp(mouseViewportPos.y, minViewportPadding, maxViewportPadding);

        // 3. Kembalikan koordinat Viewport yang sudah dikunci menjadi koordinat Screen/Pixel kembali
        Vector3 clampedMouseScreenPos = cam.ViewportToScreenPoint(mouseViewportPos);

        // 4. Tembakkan Ray menggunakan posisi layar yang sudah dikunci tadi, bukan menggunakan Input.mousePosition mentah
        ray = cam.ScreenPointToRay(clampedMouseScreenPos);

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
        if (Input.GetButtonDown("Payload"))
        {
            PayloadFireDetected = true;
            controlledAircraft.FirePayload();
        }
        if (Input.GetButtonDown("Change Payload"))
        {
            controlledAircraft.SwitchPayload();
        }

        if (Input.GetButton("Gun"))
        {
            PrimaryFireDetected = true;
        }
    }
}