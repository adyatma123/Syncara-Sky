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
    // FIX: Changed to public property for debug access.
    public List<PayloadSlot> ProcessedPayloadSlots => processedPayloadSlots;
    private List<PayloadSlot> processedPayloadSlots = new List<PayloadSlot>();

    // FIX: Changed to public property for debug access.
    public int CurrentPayloadIndex => currentPayloadIndex;
    private int currentPayloadIndex = 0;

    /// <summary>
    /// Debug Accessor: Returns the number of unique payload types equipped.
    /// </summary>
    public int GetTotalUniquePayloads() => processedPayloadSlots.Count;

    /// <summary>
    /// Debug Accessor: Returns the remaining ammo for the currently selected unique payload type.
    /// </summary>
    public int GetCurrentPayloadAmmoLeft()
    {
        if (processedPayloadSlots.Count == 0) return 0;
        return processedPayloadSlots[currentPayloadIndex].currentAmmo;
    }

    /// <summary>
    /// Debug Accessor: Returns a list of all unique payload names.
    /// </summary>
    public List<string> GetPayloadNames()
    {
        return processedPayloadSlots.Select(s => s.payload?.payloadName ?? "N/A").ToList();
    }


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
    /// It cycles through all combined hardpoints for the selected payload type,
    /// or fires from all hardpoints simultaneously if multiTargets is enabled.
    /// </summary>
    public void FireCurrentPayload()
    {
        if (processedPayloadSlots.Count == 0) return;

        PayloadSlot currentSlot = processedPayloadSlots[currentPayloadIndex];
        Payload currentPayload = currentSlot.payload;

        // Check for nulls and if reloading is in progress or if the fire rate cooldown is active
        if (currentPayload == null || currentSlot.isReloading || Time.time < currentSlot.nextFireTime) return;

        // Check if a hardpoint is available
        if (currentSlot.hardpoints.Length == 0)
        {
            Debug.LogError($"Payload slot '{currentPayload.payloadName}' has no hardpoints assigned after processing.");
            return;
        }

        // --- Determine how to fire based on multiTargets property ---
        List<Transform> launchPoints = new List<Transform>();
        int ammoCost = 0;
        int initialIndex = currentSlot.currentHardpointIndex;

        if (currentPayload.isMissile && currentPayload.multiTargets)
        {
            // Multi-target missile: Fire from ALL available hardpoints.
            launchPoints.AddRange(currentSlot.hardpoints);
            ammoCost = currentSlot.hardpoints.Length;
        }
        else
        {
            // Standard payload (rocket or single-target missile): Fire from the next hardpoint in sequence.
            launchPoints.Add(currentSlot.hardpoints[initialIndex]);
            ammoCost = 1;
        }

        // Final check if we have enough ammo to cover the launch
        if (currentSlot.currentAmmo < ammoCost)
        {
            Debug.Log($"Out of ammo for {currentPayload.payloadName}. Cannot fire {ammoCost} rounds. Remaining: {currentSlot.currentAmmo}");
            return;
        }

        // --- Execute Launches ---
        int launchesCount = 0;
        foreach (Transform spawnPoint in launchPoints)
        {
            // Only fire if the hardpoint is not empty (i.e., we are not cycling past the available ammo if multiTargets is false)
            // Note: For multiTargets=true, the ammo check above ensures we have enough.
            if (currentPayload.isMissile && currentPayload.multiTargets)
            {
                // Instantiate the payload prefab
                GameObject newPayload = Instantiate(currentPayload.payloadPrefab, spawnPoint.position, spawnPoint.rotation);
                InitializePayload(newPayload, currentPayload);
                launchesCount++;
            }
            else if (spawnPoint == currentSlot.hardpoints[initialIndex]) // Only fire the single, designated hardpoint
            {
                // Instantiate the payload prefab
                GameObject newPayload = Instantiate(currentPayload.payloadPrefab, spawnPoint.position, spawnPoint.rotation);
                InitializePayload(newPayload, currentPayload);
                launchesCount++;
            }

            // Adjust for rocket pods if applicable (only needs to be instantiated once per physical slot, but for simplicity here we keep the check)
            if (currentPayload.podPrefab != null && !currentPayload.isMissile)
            {
                Instantiate(currentPayload.podPrefab, spawnPoint.position, spawnPoint.rotation, transform);
            }
        }

        // Decrease ammo count
        currentSlot.currentAmmo -= launchesCount;

        // Set the cooldown for the next shot (cooldown is only applied once, regardless of simultaneous launches)
        currentSlot.nextFireTime = Time.time + currentPayload.reloadTime;

        // --- SOUND LOGIC ADDITION ---
        if (SoundManager.Instance != null)
        {
            string soundKey = currentPayload.isMissile ? "Player Missile" : "Player Rocket";
            SoundManager.Instance.PlaySFX(soundKey);
        }
        // ----------------------------

        Debug.Log($"Fired {currentPayload.payloadName}. Rounds launched: {launchesCount}. Ammo remaining: {currentSlot.currentAmmo}");

        // Increment hardpoint index only if it was a single launch (multi-target launch resets or maintains its current state)
        if (!currentPayload.multiTargets || !currentPayload.isMissile)
        {
            currentSlot.currentHardpointIndex = (currentSlot.currentHardpointIndex + 1) % currentSlot.hardpoints.Length;
        }

        // Start reload coroutine if out of combined ammo
        if (currentSlot.currentAmmo <= 0)
        {
            StartCoroutine(ReloadPayload(currentSlot));
        }
    }

    /// <summary>
    /// Helper method to instantiate and initialize the payload projectile.
    /// </summary>
    private void InitializePayload(GameObject newPayload, Payload currentPayload)
    {
        Rocket rocketScript = newPayload.GetComponent<Rocket>();
        if (rocketScript != null)
        {
            // Call a dedicated public method on Rocket.cs to set its private properties.
            rocketScript.SetPayloadData(currentPayload.speed, currentPayload.damage, currentPayload.lifeTime);

            // Note: Additional missile-specific properties (lockRadius, etc.) might need to be passed here
            // if the Rocket script handles homing logic.
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
        // NOTE: The total ammo logic here assumes each original slot instance contributed 'maxAmmo' to the total.
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
