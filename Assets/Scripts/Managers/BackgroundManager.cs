using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages a continuously scrolling 3D background by recycling segments.
/// This prevents continuous instantiation/destruction for better performance.
/// </summary>
public class BackgroundManager : MonoBehaviour
{
    // Inner class to track segments waiting for recycling
    private class RecycleDelaySegment
    {
        public GameObject segment;
        public float remainingTime;

        public RecycleDelaySegment(GameObject seg, float time)
        {
            segment = seg;
            remainingTime = time;
        }
    }

    [Header("Configuration")]
    [Tooltip("The prefab for one segment of the background.")]
    public GameObject backgroundPrefab;
    [Tooltip("The speed at which the background moves toward the player.")]
    public float scrollSpeed = 5f;
    [Tooltip("The number of segments needed to seamlessly fill the view (usually 2 or 3).")]
    public int segmentCount = 3;
    [Tooltip("The time (in seconds) to wait after a segment moves off-screen before recycling it to the far end.")]
    public float recycleDelayTime = 2f; // NEW: Public variable for the delay

    private float segmentLength;
    private List<GameObject> activeSegments = new List<GameObject>();
    private List<RecycleDelaySegment> delayedSegments = new List<RecycleDelaySegment>(); // NEW: List for segments in the delay queue
    private Transform cameraTransform;

    void Start()
    {
        cameraTransform = Camera.main.transform;

        // 1. Calculate the length of a single segment from its Renderer bounds.
        Renderer renderer = backgroundPrefab.GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            Debug.LogError("Background prefab must have a Renderer component to calculate its length.", this);
            enabled = false;
            return;
        }

        segmentLength = renderer.bounds.size.z;

        // 2. Initial setup: Spawn all required segments.
        InitializeSegments();
    }

    /// <summary>
    /// Spawns the initial set of background segments and positions them end-to-end.
    /// </summary>
    void InitializeSegments()
    {
        Vector3 currentSpawnPosition = transform.position;

        for (int i = 0; i < segmentCount; i++)
        {
            // Position each segment sequentially
            Vector3 spawnPosition = currentSpawnPosition + Vector3.forward * (i * segmentLength);

            // Instantiate the segment as a child of the manager object
            GameObject newSegment = Instantiate(backgroundPrefab, spawnPosition, transform.rotation, transform);
            activeSegments.Add(newSegment);
        }
    }

    void Update()
    {
        // Calculate movement distance for this frame
        float moveDistance = scrollSpeed * Time.deltaTime;

        // 1. Check for segments that have moved off-screen and add them to the delay queue
        if (activeSegments.Count > 0)
        {
            // The segment at index 0 is always the oldest (closest to the camera)
            GameObject frontSegment = activeSegments[0];

            // Reset threshold is when the segment's back edge (center Z - half length) passes the camera Z position.
            float resetThreshold = cameraTransform.position.z - (segmentLength / 2f);

            if (frontSegment.transform.position.z < resetThreshold)
            {
                // --- QUEUE FOR DELAYED RECYCLE ---

                // Add the segment to the delay list
                delayedSegments.Add(new RecycleDelaySegment(frontSegment, recycleDelayTime));

                // Remove it from the active list
                activeSegments.RemoveAt(0);
            }
        }

        // 2. Apply continuous movement to all ACTIVE segments
        foreach (GameObject bg in activeSegments)
        {
            if (bg != null)
            {
                // Move in World space to ignore manager's rotation
                bg.transform.Translate(Vector3.back * moveDistance, Space.World);
            }
        }

        // 3. Update the delay queue, apply movement, and recycle timed-out segments
        // We use a reverse loop to safely remove items from the list.
        for (int i = delayedSegments.Count - 1; i >= 0; i--)
        {
            RecycleDelaySegment delayItem = delayedSegments[i];
            GameObject delayedSegment = delayItem.segment;

            if (delayedSegment != null)
            {
                // Apply continuous movement to delayed segments as well
                delayedSegment.transform.Translate(Vector3.back * moveDistance, Space.World);

                // Decrease the remaining delay time
                delayItem.remainingTime -= Time.deltaTime;

                if (delayItem.remainingTime <= 0f)
                {
                    // --- RECYCLE (Delay is finished) ---

                    // a. Find the furthest Z position (end of the loop)
                    float newSpawnZ;

                    // If there are active segments, spawn after the last one
                    if (activeSegments.Count > 0)
                    {
                        GameObject lastActiveSegment = activeSegments[activeSegments.Count - 1];
                        newSpawnZ = lastActiveSegment.transform.position.z + segmentLength;
                    }
                    // Otherwise, spawn after the last segment that *was* active (which is the one being recycled)
                    // This scenario is unlikely with segmentCount > 1, but handles edge cases.
                    else
                    {
                        newSpawnZ = delayedSegment.transform.position.z + segmentLength;
                    }

                    // b. Reposition the segment to the far end
                    delayedSegment.transform.position = new Vector3(transform.position.x, transform.position.y, newSpawnZ);

                    // c. Move from delay queue back to the active list (at the end)
                    activeSegments.Add(delayedSegment);
                    delayedSegments.RemoveAt(i);
                }
            }
            else
            {
                // Segment was destroyed externally (error case), remove from list
                delayedSegments.RemoveAt(i);
            }
        }
    }
}