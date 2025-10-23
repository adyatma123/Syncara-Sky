using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Controls a horizontal ScrollRect to snap content to the nearest item when scrolling stops.
/// Now handles both GunSelector and PayloadSelector.
/// </summary>
public class SnapToItem : MonoBehaviour
{
    // NEW: Reference to the GunSelector to call SelectGunByIndex on snap completion.
    [Header("Dependencies")]
    public GunSelector gunSelector;
    public PayloadSelector payloadSelector; // NEW: Reference for Payload Selector

    [Header("UI References")]
    public ScrollRect scrollRect;
    public RectTransform contentPanel;
    public RectTransform itemPrefab; // Used to get item width (Must be set from the first child)
    public HorizontalLayoutGroup HLG;

    private bool isCurrentlySnapping = false;
    float snapSpeed;

    private int targetItemIndex = -1;
    private int lastSnappedIndex = -1; // Added for better tracking of selection calls

    [Header("Snapping Settings")]
    [Tooltip("The force (speed) with which the panel snaps to the item.")]
    public float snappingForce = 15f;

    [Header("Scaling Settings")]
    [Tooltip("The scale factor for the snapped item (1.0 is original size).")]
    public float snappedScaleMultiplier = 1.2f;

    [Tooltip("The scale factor for the other items (1.0 is original size).")]
    public float defaultScaleMultiplier = 0.8f;

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
        if (itemRects.Count == 0 || itemPrefab == null)
        {
            // Re-populate itemRects in case Start() failed or items were added later (e.g. dynamic items)
            if (itemRects.Count == 0 && contentPanel.childCount > 0)
            {
                itemRects.Clear();
                foreach (Transform child in contentPanel)
                {
                    if (child.TryGetComponent<RectTransform>(out RectTransform rect))
                    {
                        itemRects.Add(rect);
                    }
                }

                // Re-calculate spacing if necessary (assuming itemPrefab is set externally/on start)
                if (itemPrefab != null)
                {
                    itemWidth = itemPrefab.rect.width;
                    if (HLG != null) spacingAndWidth = itemWidth + HLG.spacing;
                }
            }
            if (itemRects.Count == 0) return;
        }

        // Determine the item index to snap to. 
        int calculatedItem = targetItemIndex != -1
            ? targetItemIndex
            : Mathf.RoundToInt((0 - contentPanel.localPosition.x) / spacingAndWidth);

        // --- NEW: Bounds Checking for Snapping ---
        int currentItem = Mathf.Clamp(calculatedItem, 0, itemRects.Count - 1);

        // Call SetSelectedIndex on the active selector
        if (gunSelector != null && gunSelector.isActiveAndEnabled &&
        (payloadSelector == null || !payloadSelector.isActiveAndEnabled))
        {
            if (currentItem >= 0 && gunSelector.availableGuns != null && currentItem < gunSelector.availableGuns.Length)
            {
                gunSelector.SetSelectedIndex(currentItem);
                gunSelector.SelectGunByIndex(currentItem);
            }
        }
        else if (payloadSelector != null && payloadSelector.isActiveAndEnabled &&
                 (gunSelector == null || !gunSelector.isActiveAndEnabled))
        {
            if (currentItem >= 0 && payloadSelector.availablePayloads != null && currentItem < payloadSelector.availablePayloads.Length)
            {
                payloadSelector.SetSelectedIndex(currentItem);
                payloadSelector.SelectPayloadByIndex(currentItem);
            }
        }

        // --- Snapping Logic ---
        if (scrollRect.velocity.magnitude < MIN_VELOCITY_FOR_SNAPPING || targetItemIndex != -1)
        {
            scrollRect.velocity = Vector2.zero;
            snapSpeed += snappingForce * Time.deltaTime;

            float targetX = 0 - (currentItem * spacingAndWidth);

            if (Mathf.Abs(contentPanel.localPosition.x - targetX) > SNAP_TOLERANCE)
            {
                isCurrentlySnapping = true;
            }

            contentPanel.localPosition = new Vector3(
                Mathf.MoveTowards(contentPanel.localPosition.x, targetX, snapSpeed),
                contentPanel.localPosition.y,
                contentPanel.localPosition.z);

            if (Mathf.Abs(contentPanel.localPosition.x - targetX) < SNAP_TOLERANCE)
            {
                // Pemicu 2: Cek apakah snap BARU saja selesai DAN index berubah
                if (isCurrentlySnapping || currentItem != lastSnappedIndex)
                {
                    targetItemIndex = -1; // Reset target index once snapped
                    snapSpeed = 0; // Reset snap speed

                    // Call the selection logic on the active selector upon successful snap.
                    if (currentItem != lastSnappedIndex)
                    {
                        if (gunSelector != null && gunSelector.isActiveAndEnabled)
                        {
                            gunSelector.SelectGunByIndex(currentItem);
                        }
                        else if (payloadSelector != null && payloadSelector.isActiveAndEnabled)
                        {
                            if (payloadSelector.availablePayloads != null &&
                                currentItem >= 0 && currentItem < payloadSelector.availablePayloads.Length)
                            {
                                payloadSelector.SetSelectedIndex(currentItem);
                                payloadSelector.SelectPayloadByIndex(currentItem);
                            }
                        }
                    }

                    // *** FIX: Hanya putar suara sekali saat snap benar-benar selesai ***
                    if (isCurrentlySnapping && SoundManager.Instance != null)
                    {
                        SoundManager.Instance.PlaySFX("Click");
                    }

                    lastSnappedIndex = currentItem;
                    isCurrentlySnapping = false;
                }
            }
        }
        else if (scrollRect.velocity.magnitude >= MIN_VELOCITY_FOR_SNAPPING)
        {
            targetItemIndex = -1;
            snapSpeed = 0;
            isCurrentlySnapping = false;
        }

        // --- Scaling Logic (Continuous) ---
        ScaleItems();
    }

    /// <summary>
    /// Public function to be called by a Button's OnClick() event.
    /// Initiates a smooth snap to the item corresponding to the given index.
    /// </summary>
    public void OnItemClick(int itemIndex)
    {
        // Clamp the itemIndex to ensure it's a valid target
        itemIndex = Mathf.Clamp(itemIndex, 0, itemRects.Count - 1);

        if (itemIndex >= 0 && itemIndex < itemRects.Count)
        {
            targetItemIndex = itemIndex;
            snapSpeed = 0;

            // Stop current scroll velocity immediately
            scrollRect.velocity = Vector2.zero;
        }
    }

    /// <summary>
    /// Adjusts the scale of all items based on their distance from the scroll view's center.
    /// </summary>
    private void ScaleItems()
    {
        float centerOffset = contentPanel.localPosition.x;
        float maxDistance = spacingAndWidth * maxScaleDistanceMultiplier;

        for (int i = 0; i < itemRects.Count; i++)
        {
            RectTransform itemRect = itemRects[i];

            float itemCenterPos = i * spacingAndWidth;
            float distance = Mathf.Abs(itemCenterPos + centerOffset);
            float normalizedDistance = Mathf.Clamp01(distance / maxDistance);
            float targetScale = Mathf.Lerp(snappedScaleMultiplier, defaultScaleMultiplier, normalizedDistance);

            itemRect.localScale = Vector3.Lerp(itemRect.localScale, Vector3.one * targetScale, Time.deltaTime * scaleSmoothness);
        }
    }
}
