using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Identifies a single Payload item in the scroll view, connects it to its ScriptableObject data,
/// and handles selection feedback + snapping interaction.
/// </summary>
public class PayloadItemIdentifier : MonoBehaviour
{
    [Header("Core References")]
    private PayloadSelector payloadSelector;

    [Tooltip("Index of this item in the availablePayloads array.")]
    public int ItemIndex = -1;

    [Tooltip("The data this item represents.")]
    public Payload PayloadData;

    [Header("Visual Components")]
    public Button button;
    public TextMeshProUGUI nameText;
    public GameObject SelectionHighlight; // Optional highlight (like border/outline)

    public bool IsSelected { get; private set; } = false;

    void Awake()
    {
        ItemIndex = transform.GetSiblingIndex();

        payloadSelector = FindObjectOfType<PayloadSelector>();
        if (payloadSelector == null)
        {
            Debug.LogError("[PayloadItemIdentifier] PayloadSelector not found in scene!");
            return;
        }

        // Setup UI display if data is already linked
        if (PayloadData != null && nameText != null)
            nameText.text = PayloadData.payloadName;

        // Setup button snapping
        if (button == null)
            button = GetComponent<Button>();

        if (button != null && payloadSelector.snapToItem != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => payloadSelector.snapToItem.OnItemClick(ItemIndex));
        }

        // Optional: disable highlight initially
        if (SelectionHighlight != null)
            SelectionHighlight.SetActive(false);
    }

    /// <summary>
    /// Called by PayloadSelector to initialize this item with the correct data.
    /// </summary>
    public void Initialize(PayloadSelector selector, int index, Payload data)
    {
        payloadSelector = selector;
        ItemIndex = index;
        PayloadData = data;

        if (nameText != null)
            nameText.text = data.payloadName;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => payloadSelector.snapToItem.OnItemClick(ItemIndex));
        }

        SetSelectedStatus(false);
    }

    /// <summary>
    /// Called by PayloadSelector or SnapToItem when selection changes.
    /// </summary>
    public void SetSelectedStatus(bool status)
    {
        IsSelected = status;
        if (SelectionHighlight != null)
            SelectionHighlight.SetActive(status);
    }
}
