// GameSelectionManager.cs (Revised for Slot Count)
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A persistent Singleton that holds selected game state data across scenes.
/// </summary>
public class GameSelectionManager : MonoBehaviour
{
    public static GameSelectionManager Instance { get; private set; }

    public Guns ConfirmedGunSelection { get; private set; }
    public Payload[] ConfirmedPayloadSelections { get; private set; }
    public Vehicles ConfirmedVehicle { get; private set; }

    // NEW: Jumlah slot yang diambil dari PayloadManager kendaraan yang dipilih.
    public int VehiclePayloadSlotCount { get; private set; } = 2; // Default ke 4

    private void Awake()
    {
        // Singleton setup: Ensure only one instance exists and persists.
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Inisialisasi default
            ConfirmedPayloadSelections = new Payload[VehiclePayloadSlotCount];
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetConfirmedVehicle(Vehicles vehicleData)
    {
        ConfirmedVehicle = vehicleData;
        Debug.Log($"Selection Manager: Confirmed vehicle set to {vehicleData.name} (Tier {vehicleData.Tier}).");
    }

    /// <summary>
    /// PUBLIC: Called by GunSelector when the player confirms their choice.
    /// </summary>
    /// <param name="gunData">The ScriptableObject of the selected gun.</param>
    public void SetConfirmedGun(Guns gunData)
    {
        ConfirmedGunSelection = gunData;
        Debug.Log($"Selection Manager: Confirmed gun set to {gunData.name}.");
    }

    /// <summary>
    /// NEW: Called by VhcChgr to set the confirmed payload slot count.
    /// </summary>
    /// <param name="count">The number of payload slots available on the vehicle.</param>
    public void SetVehiclePayloadSlotCount(int count)
    {
        VehiclePayloadSlotCount = count;
        Debug.Log($"Selection Manager: Payload Slot Count set to {count}.");

        // KRUSIAL: Hanya buat array baru jika array yang ada 'null' ATAU ukurannya BERBEDA.
        // Jika ukurannya SAMA, kita biarkan array yang ada (beserta isinya)
        if (ConfirmedPayloadSelections == null || ConfirmedPayloadSelections.Length != count)
        {
            Debug.Log($"Selection Manager: Array size mismatch (was {ConfirmedPayloadSelections?.Length ?? -1}). Creating new array for {count} slots.");

            // Simpan data lama sementara
            Payload[] oldPayloads = ConfirmedPayloadSelections;

            // Buat array baru
            ConfirmedPayloadSelections = new Payload[count];

            // Coba salin data lama sebanyak mungkin (berguna jika ganti dari 4 slot ke 2 slot)
            if (oldPayloads != null)
            {
                int slotsToCopy = Mathf.Min(oldPayloads.Length, ConfirmedPayloadSelections.Length);
                for (int i = 0; i < slotsToCopy; i++)
                {
                    ConfirmedPayloadSelections[i] = oldPayloads[i];
                }
                Debug.Log($"Selection Manager: Copied {slotsToCopy} old payload(s) to new array.");
            }
        }
        else
        {
            Debug.Log($"Selection Manager: Array size {count} matches. Retaining existing payloads.");
        }
    }


    /// <summary>
    /// Called by PayloadSelector when the player confirms their loadout.
    /// </summary>
    /// <param name="payloads">The array of Payload ScriptableObjects for all slots.</param>
    public void SetConfirmedPayloads(Payload[] payloads)
    {
        ConfirmedPayloadSelections = payloads;
        Debug.Log($"Selection Manager: Confirmed payload loadout set with {payloads.Length} slots.");
    }
}
