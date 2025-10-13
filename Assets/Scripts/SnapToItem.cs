using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Controls a horizontal ScrollRect to snap content to the nearest item when scrolling stops.
/// Now includes item scaling: the snapped item becomes larger, and others become smaller.
/// </summary>
public class SnapToItem : MonoBehaviour
{
    // NEW: Reference to the GunSelector to call SelectGunByIndex on snap completion.
    [Header("Dependencies")]
    public GunSelector gunSelector;

    [Header("UI References")]
    public ScrollRect scrollRect;
    public RectTransform contentPanel;
    public RectTransform itemPrefab; // Used to get item width
    public HorizontalLayoutGroup HLG;

    // bool isSnapped; // REMOVED: This non-functional variable is removed as requested.

    public float snapForce;
    float snapSpeed;

    private int targetItemIndex = -1; // New variable to track the index we are snapping to via click

    [Header("Snapping Settings")]
    [Tooltip("The force (speed) with which the panel snaps to the item.")]
    public float snappingForce = 15f;

    [Header("Scaling Settings")]
    [Tooltip("The scale factor for the snapped item (1.0 is original size).")]
    public float snappedScaleMultiplier = 1.2f;

    [Tooltip("The scale factor for the other items (1.0 is original size).")]
    public float defaultScaleMultiplier = 0.8f;

    // FIX 1: Moving 'scaleSmoothness' declaration from the bottom of the section to fix 'does not exist in current context' error.
    [Tooltip("The speed at which the scale changes for a smooth visual transition.")]
    public float scaleSmoothness = 10f;

    [Tooltip("How far (in multiples of item width) the item's center can be before it fully returns to the default scale.")]
    public float maxScaleDistanceMultiplier = 1.5f;


    // Internal state variables
    private float itemWidth;
    private float spacingAndWidth;
    private List<RectTransform> itemRects = new List<RectTransform>();

    // Constants
    private const float MIN_VELOCITY_FOR_SNAPPING = 200f;
    private const float SNAP_TOLERANCE = 0.01f;

    void Start()
    {
        // Calculate item width and total width + spacing, which is needed for movement calculations
        if (itemPrefab != null && HLG != null && contentPanel != null)
        {
            itemWidth = itemPrefab.rect.width;
            spacingAndWidth = itemWidth + HLG.spacing;

            // Populate the list of item RectTransforms (assuming they are direct children of the contentPanel)
            foreach (Transform child in contentPanel)
            {
                if (child.TryGetComponent<RectTransform>(out RectTransform rect))
                {
                    itemRects.Add(rect);
                }
            }

            if (itemRects.Count == 0 && contentPanel.childCount > 0)
            {
                Debug.LogWarning("Content Panel has children, but they are not RectTransforms or the Item Prefab is not set up correctly.");
            }
        }
    }

    void Update()
    {
        if (itemRects.Count == 0) return;

        // Determine the item index to snap to. 
        int calculatedItem = targetItemIndex != -1
            ? targetItemIndex
            : Mathf.RoundToInt((0 - contentPanel.localPosition.x) / spacingAndWidth);

        // --- NEW: Bounds Checking for Snapping ---
        int currentItem = Mathf.Clamp(calculatedItem, 0, itemRects.Count - 1);

        // Always call the status updater in GunSelector to handle isSelected toggle
        if (gunSelector != null)
        {
            gunSelector.SetSelectedIndex(currentItem);
        }

        // --- Snapping Logic ---
        // Check if velocity is low (scroll ended) OR if a button click initiated a snap (targetItemIndex != -1)
        if (scrollRect.velocity.magnitude < MIN_VELOCITY_FOR_SNAPPING || targetItemIndex != -1)
        {
            scrollRect.velocity = Vector2.zero;
            snapSpeed += snappingForce * Time.deltaTime;

            // Calculate the target X position for the content panel to snap to the center of the clamped 'currentItem'.
            float targetX = 0 - (currentItem * spacingAndWidth);

            contentPanel.localPosition = new Vector3(
                Mathf.MoveTowards(contentPanel.localPosition.x, targetX, snapSpeed),
                contentPanel.localPosition.y,
                contentPanel.localPosition.z);

            if (Mathf.Abs(contentPanel.localPosition.x - targetX) < SNAP_TOLERANCE)
            {
                // isSnapped = true; // No longer needed
                targetItemIndex = -1; // Reset target index once snapped
                snapSpeed = 0; // Reset snap speed

                // Call the selection logic on the GunSelector upon successful snap.
                if (gunSelector != null)
                {
                    gunSelector.SelectGunByIndex(currentItem);
                }
            }
        }
        else if (scrollRect.velocity.magnitude >= MIN_VELOCITY_FOR_SNAPPING)
        {
            // isSnapped = false; // No longer needed
            targetItemIndex = -1; // Ensure click snap is cancelled if the user starts scrolling
            snapSpeed = 0;
        }

        // --- Scaling Logic (Continuous) ---
        ScaleItems();
    }

    /// <summary>
    /// Public function to be called by a Button's OnClick() event.
    /// Initiates a smooth snap to the item corresponding to the given index.
    /// </summary>
    /// <param name="itemIndex">The index of the item to snap to (0-based).</param>
    public void OnItemClick(int itemIndex)
    {
        // Clamp the itemIndex to ensure it's a valid target
        itemIndex = Mathf.Clamp(itemIndex, 0, itemRects.Count - 1);

        if (itemIndex >= 0 && itemIndex < itemRects.Count)
        {
            // Set the target index and reset state to start the snap in Update()
            targetItemIndex = itemIndex;
            // isSnapped = false; // No longer needed
            snapSpeed = 0;

            // Stop current scroll velocity immediately
            scrollRect.velocity = Vector2.zero;
        }
    }

    /// <summary>
    /// Adjusts the scale of all items based on their distance from the scroll view's center,
    /// creating a smooth zoom-in/zoom-out effect.
    /// </summary>
    private void ScaleItems()
    {
        // The center point of the viewport in the content panel's frame of reference is 0.
        float centerOffset = contentPanel.localPosition.x;

        // The distance threshold at which the item's scale should be fully at the defaultScaleMultiplier.
        float maxDistance = spacingAndWidth * maxScaleDistanceMultiplier;

        for (int i = 0; i < itemRects.Count; i++)
        {
            RectTransform itemRect = itemRects[i];

            // Item's calculated center position relative to the contentPanel's anchor.
            float itemCenterPos = i * spacingAndWidth;

            // Current distance of item i's center from the viewport center (0).
            float distance = Mathf.Abs(itemCenterPos + centerOffset);

            // Normalized distance: 0 (at center) to 1 (at maxDistance)
            float normalizedDistance = Mathf.Clamp01(distance / maxDistance);

            // Scale factor: smooth transition from snappedScaleMultiplier (0) to defaultScaleMultiplier (1)
            float targetScale = Mathf.Lerp(snappedScaleMultiplier, defaultScaleMultiplier, normalizedDistance);

            // Apply the scale smoothly using Lerp
            itemRect.localScale = Vector3.Lerp(itemRect.localScale, Vector3.one * targetScale, Time.deltaTime * scaleSmoothness);
        }
    }
}
