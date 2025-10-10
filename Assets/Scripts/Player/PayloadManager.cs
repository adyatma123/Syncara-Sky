using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Required for Linq queries

/// <summary>
/// A class representing a single payload slot on the aircraft.
/// </summary>
[System.Serializable]
public class PayloadSlot
{
    [Tooltip("The payload assigned to this slot.")]
    // Corrected back to the generic Payload ScriptableObject type
    public Payload payload;

    [Tooltip("The launch points associated with this payload slot. These will be merged if payloads are identical.")]
    public Transform[] hardpoints;

    // Runtime data for the slot
    [HideInInspector] public int currentAmmo;
    [HideInInspector] public bool isReloading = false;
    [HideInInspector] public float nextFireTime = 0f;
    [HideInInspector] public int currentHardpointIndex = 0;
}

/// <summary>
/// Manages the aircraft's equipped payloads, including ammo, reloading, and switching.
/// Payloads of the same type (ScriptableObject) will be combined into a single slot 
/// with all hardpoints sequenced together.
/// </summary>
public class PayloadManager : MonoBehaviour
{
    [Header("Payload Inventory")]
    [Tooltip("The list of payload slots equipped on this aircraft. Identical payloads are automatically combined.")]
    public PayloadSlot[] payloadSlots;

    // The list of slots after processing (only unique payload types remain here)
    private List<PayloadSlot> processedPayloadSlots = new List<PayloadSlot>();

    private int currentPayloadIndex = 0;

    /// <summary>
    /// Initializes the payload manager by merging identical payloads and populating ammo counts.
    /// </summary>
    void Start()
    {
        if (payloadSlots.Length == 0)
        {
            Debug.LogWarning("PayloadManager has no equipped payloads. Please add them in the Inspector.");
            return;
        }

        ProcessPayloadSlots();
    }

    /// <summary>
    /// Processes the raw payloadSlots array to merge all identical payloads into single slots.
    /// This combines hardpoints and aggregates max ammo.
    /// </summary>
    private void ProcessPayloadSlots()
    {
        // Group all slots by their Payload ScriptableObject reference
        var groupedSlots = payloadSlots
            .Where(slot => slot.payload != null)
            .GroupBy(slot => slot.payload);

        foreach (var group in groupedSlots)
        {
            Payload uniquePayload = group.Key;

            // 1. Combine all hardpoints from the group into one list
            List<Transform> allHardpoints = new List<Transform>();
            foreach (var slot in group)
            {
                if (slot.hardpoints != null)
                {
                    allHardpoints.AddRange(slot.hardpoints);
                }
            }

            // 2. Create the new, combined PayloadSlot
            PayloadSlot newSlot = new PayloadSlot
            {
                payload = uniquePayload,
                hardpoints = allHardpoints.ToArray(), // Array of all combined hardpoints

                // IMPORTANT: Calculate total ammo across all combined slots
                currentAmmo = uniquePayload.maxAmmo * group.Count(),
            };

            processedPayloadSlots.Add(newSlot);
            Debug.Log($"Combined Payload: {uniquePayload.payloadName}. Total Hardpoints: {newSlot.hardpoints.Length}. Initial Ammo: {newSlot.currentAmmo}");
        }

        // Handle case where all slots were null
        if (processedPayloadSlots.Count == 0)
        {
            Debug.LogWarning("All payload slots were empty or contained null Payload ScriptableObjects.");
            return;
        }
    }

    /// <summary>
    /// Switches to the next UNIQUE payload in the equipped list.
    /// </summary>
    public void SwitchPayload()
    {
        if (processedPayloadSlots.Count <= 1) return;

        currentPayloadIndex = (currentPayloadIndex + 1) % processedPayloadSlots.Count;
        Debug.Log($"Switched to unique payload: {processedPayloadSlots[currentPayloadIndex].payload.payloadName}");
    }

    /// <summary>
    /// Fires the currently equipped payload, checking for reload time and ammo.
    /// It cycles through all combined hardpoints for the selected payload type.
    /// </summary>
    public void FireCurrentPayload()
    {
        if (processedPayloadSlots.Count == 0) return;

        PayloadSlot currentSlot = processedPayloadSlots[currentPayloadIndex];
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
            Debug.LogError($"Payload slot '{currentPayload.payloadName}' has no hardpoints assigned after processing.");
            return;
        }

        // Determine the next launch point based on the slot's index
        Transform spawnPoint = currentSlot.hardpoints[currentSlot.currentHardpointIndex];

        // Instantiate the payload prefab
        GameObject newPayload = Instantiate(currentPayload.payloadPrefab, spawnPoint.position, spawnPoint.rotation);

        // --- CRITICAL FIX: INITIALIZE ROCKET PROPERTIES VIA METHOD ---
        Rocket rocketScript = newPayload.GetComponent<Rocket>();
        if (rocketScript != null)
        {
            // Call a dedicated public method on Rocket.cs to set its private properties.
            // This assumes Rocket.cs uses the SetPayloadData method (speed, damage, lifeTime).
            rocketScript.SetPayloadData(currentPayload.speed, currentPayload.damage, currentPayload.lifeTime);
        }

        // Adjust for rocket pods if applicable
        if (currentPayload.podPrefab != null)
        {
            // This should only run if the Payload is NOT a missile, but assuming 'podPrefab' presence is the check:
            if (!currentPayload.isMissile)
            {
                Instantiate(currentPayload.podPrefab, spawnPoint.position, spawnPoint.rotation, transform);
            }
        }

        // Decrease ammo count
        currentSlot.currentAmmo--;

        // Set the cooldown for the next shot
        currentSlot.nextFireTime = Time.time + currentPayload.reloadTime;

        // Play shoot sound
        // NOTE: SoundManager.Instance is assumed to exist
        // if (SoundManager.Instance != null) { SoundManager.Instance.PlaySFX(currentPayload.shootSound); }

        Debug.Log($"Fired {currentPayload.payloadName} from hardpoint {currentSlot.currentHardpointIndex}. Ammo remaining: {currentSlot.currentAmmo}");

        // Increment hardpoint index to cycle to the next physical mount point
        currentSlot.currentHardpointIndex = (currentSlot.currentHardpointIndex + 1) % currentSlot.hardpoints.Length;

        // Start reload coroutine if out of combined ammo
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

        // Use the Payload's reload time for the wait duration
        yield return new WaitForSeconds(slotToReload.payload.reloadTime);

        // When reloading, we restore the ammo to the value of ONE Payload SO's maxAmmo, 
        // multiplied by the total number of hardpoints associated with this payload type.
        slotToReload.currentAmmo = slotToReload.payload.maxAmmo * slotToReload.hardpoints.Length;

        slotToReload.isReloading = false;
        Debug.Log($"Reload complete for {slotToReload.payload.payloadName}. Ammo restored to {slotToReload.currentAmmo}.");
    }

    /// <summary>
    /// Provides access to the currently equipped payload's data.
    /// </summary>
    /// <returns>The Payload ScriptableObject currently in use.</returns>
    public Payload GetCurrentPayload()
    {
        if (processedPayloadSlots.Count == 0) return null;
        return processedPayloadSlots[currentPayloadIndex].payload;
    }
}
