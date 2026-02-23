using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class SnapToItem : MonoBehaviour
{
    public enum SnapTargetMode
    {
        None,
        Gun,
        Payload
    }

    [Header("Mode Control")]
    public SnapTargetMode currentMode = SnapTargetMode.None;

    [Header("Dependencies")]
    public GunSelector gunSelector;
    public PayloadSelector payloadSelector;

    [Header("UI References")]
    public ScrollRect scrollRect;
    public RectTransform contentPanel;
    public RectTransform itemPrefab;
    public HorizontalLayoutGroup HLG;

    private bool isCurrentlySnapping = false;
    float snapSpeed;

    private int targetVisualIndex = -1; // Index visual (0, 1, 2 dari item yang aktif saja)
    private int lastSnappedVisualIndex = -1;

    [Header("Snapping Settings")]
    public float snappingForce = 15f;

    [Header("Scaling Settings")]
    public float snappedScaleMultiplier = 1.2f;
    public float defaultScaleMultiplier = 0.8f;
    public float scaleSmoothness = 10f;
    public float maxScaleDistanceMultiplier = 1.5f;

    // BARU: Struct untuk memetakan item visual ke index data aslinya
    private struct ActiveItem
    {
        public RectTransform rect;
        public int originalIndex; // Index asli di hierarki/array data
    }

    // List hanya untuk item yang AKTIF
    private List<ActiveItem> activeItems = new List<ActiveItem>();

    private float itemWidth;
    private float spacingAndWidth;

    private const float MIN_VELOCITY_FOR_SNAPPING = 200f;
    private const float SNAP_TOLERANCE = 0.01f;

    void Start()
    {
        // Panggil refresh saat start jika memungkinkan
        UpdateActiveItems();
    }

    /// <summary>
    /// MEMPERBARUI daftar item yang aktif. Harus dipanggil oleh Selector setelah filtering selesai.
    /// </summary>
    public void UpdateActiveItems()
    {
        if (itemPrefab != null && HLG != null && contentPanel != null)
        {
            itemWidth = itemPrefab.rect.width;
            spacingAndWidth = itemWidth + HLG.spacing;

            activeItems.Clear();

            // Iterasi semua anak di contentPanel
            int childCount = contentPanel.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = contentPanel.GetChild(i);
                // Hanya masukkan ke list jika item tersebut AKTIF
                if (child.gameObject.activeSelf && child.TryGetComponent<RectTransform>(out RectTransform rect))
                {
                    // Simpan Rect dan Index Aslinya (i)
                    activeItems.Add(new ActiveItem { rect = rect, originalIndex = i });
                }
            }
        }
    }

    void Update()
    {
        // Safety check
        if (activeItems.Count == 0 || itemPrefab == null)
        {
            // Coba refresh jika list kosong tapi ada anak
            if (contentPanel.childCount > 0) UpdateActiveItems();
            if (activeItems.Count == 0) return;
        }

        // 1. Hitung posisi scroll saat ini (Visual Index)
        int calculatedVisualItem = targetVisualIndex != -1
            ? targetVisualIndex
            : Mathf.RoundToInt((0 - contentPanel.localPosition.x) / spacingAndWidth);

        // Clamp ke jumlah item yang AKTIF
        int currentVisualItem = Mathf.Clamp(calculatedVisualItem, 0, activeItems.Count - 1);

        // Ambil Data Index yang sebenarnya dari mapping
        int currentDataIndex = activeItems[currentVisualItem].originalIndex;

        // --- Logic Selection (Preview saat scroll) ---
        // Kita kirim Data Index (bukan Visual Index) ke Selector
        switch (currentMode)
        {
            case SnapTargetMode.Gun:
                if (gunSelector != null)
                {
                    // Gunakan index asli untuk set selected status
                    gunSelector.SetSelectedIndex(currentDataIndex);
                }
                break;

            case SnapTargetMode.Payload:
                if (payloadSelector != null)
                {
                    payloadSelector.SetSelectedIndex(currentDataIndex);
                }
                break;
        }

        // --- Snapping Logic ---
        if (scrollRect.velocity.magnitude < MIN_VELOCITY_FOR_SNAPPING || targetVisualIndex != -1)
        {
            scrollRect.velocity = Vector2.zero;
            snapSpeed += snappingForce * Time.deltaTime;

            // Target X berdasarkan Visual Index
            float targetX = 0 - (currentVisualItem * spacingAndWidth);

            if (Mathf.Abs(contentPanel.localPosition.x - targetX) > SNAP_TOLERANCE)
            {
                isCurrentlySnapping = true;
            }

            contentPanel.localPosition = new Vector3(
                Mathf.MoveTowards(contentPanel.localPosition.x, targetX, snapSpeed),
                contentPanel.localPosition.y,
                contentPanel.localPosition.z);

            // Cek jika Snap Selesai
            if (Mathf.Abs(contentPanel.localPosition.x - targetX) < SNAP_TOLERANCE)
            {
                if (isCurrentlySnapping || currentVisualItem != lastSnappedVisualIndex)
                {
                    targetVisualIndex = -1;
                    snapSpeed = 0;

                    // Panggil Selector Final Selection hanya jika item berubah
                    if (currentVisualItem != lastSnappedVisualIndex)
                    {
                        if (currentMode == SnapTargetMode.Gun && gunSelector != null)
                        {
                            // Kirim Data Index asli
                            gunSelector.SelectGunByIndex(currentDataIndex);
                        }
                        else if (currentMode == SnapTargetMode.Payload && payloadSelector != null)
                        {
                            payloadSelector.SelectPayloadByIndex(currentDataIndex);
                        }
                    }

                    if (isCurrentlySnapping && SoundManager.Instance != null)
                    {
                        SoundManager.Instance.PlaySFX("Click");
                    }

                    lastSnappedVisualIndex = currentVisualItem;
                    isCurrentlySnapping = false;
                }
            }
        }
        else if (scrollRect.velocity.magnitude >= MIN_VELOCITY_FOR_SNAPPING)
        {
            targetVisualIndex = -1;
            snapSpeed = 0;
            isCurrentlySnapping = false;
        }

        ScaleItems();
    }

    /// <summary>
    /// Dipanggil oleh tombol/selector. Menerima DATA INDEX (Index asli).
    /// Kita harus mencari Visual Index mana yang sesuai dengan Data Index ini.
    /// </summary>
    public void OnItemClick(int dataIndex)
    {
        // Cari Visual Index yang punya originalIndex == dataIndex
        int visualIndex = -1;
        for (int i = 0; i < activeItems.Count; i++)
        {
            if (activeItems[i].originalIndex == dataIndex)
            {
                visualIndex = i;
                break;
            }
        }

        if (visualIndex != -1)
        {
            targetVisualIndex = visualIndex; // Snap ke posisi visual yang benar
            snapSpeed = 0;
            scrollRect.velocity = Vector2.zero;
        }
        else
        {
            Debug.LogWarning($"SnapToItem: Mencoba snap ke Data Index {dataIndex} tapi item tersebut tidak aktif/tidak ditemukan.");
        }
    }

    private void ScaleItems()
    {
        float centerOffset = contentPanel.localPosition.x;
        float maxDistance = spacingAndWidth * maxScaleDistanceMultiplier;

        // Hanya scale item yang aktif
        for (int i = 0; i < activeItems.Count; i++)
        {
            RectTransform itemRect = activeItems[i].rect;
            // Posisi visual berdasarkan index visual (i)
            float itemCenterPos = i * spacingAndWidth;

            float distance = Mathf.Abs(itemCenterPos + centerOffset);
            float normalizedDistance = Mathf.Clamp01(distance / maxDistance);
            float targetScale = Mathf.Lerp(snappedScaleMultiplier, defaultScaleMultiplier, normalizedDistance);

            itemRect.localScale = Vector3.Lerp(itemRect.localScale, Vector3.one * targetScale, Time.deltaTime * scaleSmoothness);
        }
    }
}