using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A class representing a single payload slot on the aircraft.
/// </summary>
[System.Serializable]
public class PayloadSlot
{
    [Tooltip("The payload assigned to this slot.")]
    public Payload payload;
    [Tooltip("The launch points associated with this payload slot.")]
    public Transform[] hardpoints;

    // Runtime data for the slot
    [HideInInspector] public int currentAmmo;
    [HideInInspector] public bool isReloading = false;
    [HideInInspector] public float nextFireTime = 0f;
    [HideInInspector] public int currentHardpointIndex = 0;
}

/// <summary>
/// Manages the aircraft's equipped payloads, including ammo, reloading, and switching.
/// </summary>
public class PayloadManager : MonoBehaviour
{
    [Header("Payload Inventory")]
    [Tooltip("The list of payload slots equipped on this aircraft.")]
    public PayloadSlot[] payloadSlots;

    private int currentPayloadIndex = 0;

    /// <summary>
    /// Initializes the payload manager by populating ammo counts.
    /// </summary>
    void Start()
    {
        if (payloadSlots.Length == 0)
        {
            Debug.LogWarning("PayloadManager has no equipped payloads. Please add them in the Inspector.");
            return;
        }

        // Initialize ammo for each equipped payload
        foreach (var slot in payloadSlots)
        {
            if (slot.payload != null)
            {
                slot.currentAmmo = slot.payload.maxAmmo;
            }
        }
    }

    /// <summary>
    /// Switches to the next payload in the equipped list.
    /// </summary>
    public void SwitchPayload()
    {
        if (payloadSlots.Length <= 1) return;

        currentPayloadIndex = (currentPayloadIndex + 1) % payloadSlots.Length;
        Debug.Log($"Switched to payload: {payloadSlots[currentPayloadIndex].payload.payloadName}");
    }

    /// <summary>
    /// Fires the currently equipped payload, checking for reload time and ammo.
    /// </summary>
    public void FireCurrentPayload()
    {
        if (payloadSlots.Length == 0) return;

        PayloadSlot currentSlot = payloadSlots[currentPayloadIndex];
        Payload currentPayload = currentSlot.payload;

        // Check for nulls and if reloading is in progress or if the fire rate cooldown is active
        if (currentPayload == null || currentSlot.isReloading || Time.time < currentSlot.nextFireTime) return;

        // Check if we have ammo
        if (currentSlot.currentAmmo <= 0)
        {
            Debug.Log($"Out of ammo for {currentPayload.payloadName}. Cannot fire.");
            return;
        }

        // Check if a hardpoint is available
        if (currentSlot.hardpoints.Length == 0)
        {
            Debug.LogError($"Payload slot '{currentPayload.payloadName}' has no hardpoints assigned.");
            return;
        }

        // Determine the next launch point based on the slot's index
        Transform spawnPoint = currentSlot.hardpoints[currentSlot.currentHardpointIndex];

        // Instantiate the payload prefab
        GameObject newPayload = Instantiate(currentPayload.payloadPrefab, spawnPoint.position, spawnPoint.rotation);

        // Adjust for rocket pods if applicable
        if (!currentPayload.isMissile && currentPayload.podPrefab != null)
        {
            Instantiate(currentPayload.podPrefab, spawnPoint.position, spawnPoint.rotation, transform);
        }

        // Apply properties to the instantiated payload based on its type
        // This is where you would link the properties from your Payload SO to the instantiated prefab's script
        // e.g.
        // newPayload.GetComponent<HomingMissile>().Mdamage = currentPayload.damage;

        // Decrease ammo count
        currentSlot.currentAmmo--;

        // Set the cooldown for the next shot
        currentSlot.nextFireTime = Time.time + currentPayload.reloadTime;

        // Play shoot sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("Player Missile");
        }

        Debug.Log($"Fired {currentPayload.payloadName}. Ammo remaining: {currentSlot.currentAmmo}");

        // Increment hardpoint index for this slot
        currentSlot.currentHardpointIndex = (currentSlot.currentHardpointIndex + 1) % currentSlot.hardpoints.Length;

        // Start reload coroutine if out of ammo
        if (currentSlot.currentAmmo <= 0)
        {
            StartCoroutine(ReloadPayload(currentSlot));
        }
    }

    /// <summary>
    /// Coroutine to handle the reload process.
    /// </summary>
    private IEnumerator ReloadPayload(PayloadSlot slotToReload)
    {
        slotToReload.isReloading = true;
        Debug.Log($"Reloading {slotToReload.payload.payloadName}...");
        yield return new WaitForSeconds(slotToReload.payload.reloadTime);
        slotToReload.currentAmmo = slotToReload.payload.maxAmmo;
        slotToReload.isReloading = false;
        Debug.Log($"Reload complete for {slotToReload.payload.payloadName}. Ammo restored.");
    }

    /// <summary>
    /// Provides access to the currently equipped payload's data.
    /// </summary>
    /// <returns>The Payload ScriptableObject currently in use.</returns>
    public Payload GetCurrentPayload()
    {
        if (payloadSlots.Length == 0) return null;
        return payloadSlots[currentPayloadIndex].payload;
    }
}
