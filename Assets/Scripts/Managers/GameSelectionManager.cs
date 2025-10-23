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

        ConfirmedPayloadSelections = new Payload[count];
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
