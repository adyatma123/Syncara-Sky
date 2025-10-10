using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages a continuously scrolling 3D background by recycling segments.
/// This prevents continuous instantiation/destruction for better performance.
/// </summary>
public class BackgroundManager : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The prefab for one segment of the background.")]
    public GameObject backgroundPrefab;
    [Tooltip("The speed at which the background moves toward the player.")]
    public float scrollSpeed = 5f;
    [Tooltip("The number of segments needed to seamlessly fill the view (usually 2 or 3).")]
    public int segmentCount = 3;

    private float segmentLength;
    private List<GameObject> activeSegments = new List<GameObject>();
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
        if (activeSegments.Count == 0) return;

        // Calculate movement distance for this frame
        float moveDistance = scrollSpeed * Time.deltaTime;

        // The segment at index 0 is always the oldest (closest to the camera)
        GameObject frontSegment = activeSegments[0];

        // 1. Check if the front segment has moved off-screen
        // Reset threshold is when the segment's back edge (center Z - half length) passes the camera Z position.
        float resetThreshold = cameraTransform.position.z - (segmentLength / 2f);

        if (frontSegment.transform.position.z < resetThreshold)
        {
            // --- RECYCLE ---

            // a. Find the furthest Z position (end of the loop)
            GameObject lastSegment = activeSegments[activeSegments.Count - 1];
            float newSpawnZ = lastSegment.transform.position.z + segmentLength;

            // b. Reposition the oldest segment to the far end
            frontSegment.transform.position = new Vector3(transform.position.x, transform.position.y, newSpawnZ);

            // c. Update the list order: remove from front, add to back
            activeSegments.RemoveAt(0);
            activeSegments.Add(frontSegment);
        }

        // 2. Apply continuous movement to all segments
        foreach (GameObject bg in activeSegments)
        {
            if (bg != null)
            {
                // Move in World space to ignore manager's rotation
                bg.transform.Translate(Vector3.back * moveDistance, Space.World);
            }
        }
    }
}
