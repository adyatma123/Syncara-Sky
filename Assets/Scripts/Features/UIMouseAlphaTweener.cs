using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class UIMouseAlphaTweener : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Alpha Settings")]
    [Tooltip("Alpha saat mouse berada di luar UI (1 = 100% terlihat).")]
    [Range(0f, 1f)][SerializeField] private float normalAlpha = 1.0f;

    [Tooltip("Alpha saat mouse berada di atas UI (0.5 = 50% transparan).")]
    [Range(0f, 1f)][SerializeField] private float hoverAlpha = 0.5f;

    [Tooltip("Kecepatan transisi perubahan nilai transparan.")]
    [SerializeField] private float fadeSpeed = 5f;

    private CanvasGroup canvasGroup;
    private float targetAlpha;

    private void Awake()
    {
        // Mengambil atau otomatis menambahkan CanvasGroup pada objek ini
        canvasGroup = GetComponent<CanvasGroup>();

        // Set kondisi awal ke alpha normal
        canvasGroup.alpha = normalAlpha;
        targetAlpha = normalAlpha;
    }

    private void Update()
    {
        // Mengubah alpha secara halus menuju target alpha setiap frame
        if (!Mathf.Approximately(canvasGroup.alpha, targetAlpha))
        {
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        }
    }

    // Dipanggil otomatis oleh EventSystem saat mouse masuk ke area UI
    public void OnPointerEnter(PointerEventData eventData)
    {
        targetAlpha = hoverAlpha;
    }

    // Dipanggil otomatis oleh EventSystem saat mouse keluar dari area UI
    public void OnPointerExit(PointerEventData eventData)
    {
        targetAlpha = normalAlpha;
    }

    // Memastikan jika UI dimatikan/di-disable, visualnya langsung kembali normal
    private void OnDisable()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = normalAlpha;
            targetAlpha = normalAlpha;
        }
    }
}