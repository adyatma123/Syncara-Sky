using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Komponen Health Bar yang mirip Slider, tetapi dirancang untuk mengontrol
/// lebar atau tinggi SpriteRenderer yang menggunakan Draw Mode: Sliced.
/// Skrip ini tidak memiliki fitur interaksi UI.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class HealthBarSprite : MonoBehaviour
{
    // --- Enumerasi ---

    /// <summary>
    /// Arah bar kesehatan dari nilai minimum ke maksimum.
    /// </summary>
    public enum Direction
    {
        LeftToRight,
        RightToLeft,
        BottomToTop,
        TopToBottom,
    }

    [Serializable]
    /// <summary>
    /// Tipe Event yang dipanggil saat nilai (value) kesehatan berubah.
    /// </summary>
    public class HealthBarEvent : UnityEvent<float> { }

    // --- Properti yang Diperlukan (Diambil dari Slider) ---

    [Header("Fill Reference")]
    [Tooltip("Game Object yang memiliki SpriteRenderer (Draw Mode: Sliced) yang ukurannya akan diubah.")]
    [SerializeField] private GameObject m_FillObject;

    [Header("Behavior")]
    [SerializeField] private Direction m_Direction = Direction.LeftToRight;

    // PERUBAHAN: MinValue diatur ke 0.
    [SerializeField] private float m_MinValue = 0f;

    // PERUBAHAN: MaxValue diatur ke 1.
    [SerializeField] private float m_MaxValue = 1f;
    [SerializeField] private bool m_WholeNumbers = false;

    [Space]
    // PERUBAHAN: Nilai default diatur ke 1 (penuh) dalam rentang 0-1.
    [SerializeField] protected float m_Value = 1f;

    [Header("Events")]
    [SerializeField] private HealthBarEvent m_OnValueChanged = new HealthBarEvent();

    // --- Variabel Internal ---

    private SpriteRenderer m_FillRenderer;
    private float m_OriginalSize; // Lebar atau tinggi asli dari fill object
    private bool m_IsHorizontal; // True jika LeftToRight atau RightToLeft

    // --- Properti Publik ---

    public Direction direction { get => m_Direction; set { if (m_Direction != value) { m_Direction = value; UpdateLayout(); UpdateFillSize(); } } }
    public float minValue { get => m_MinValue; set { if (m_MinValue != value) { m_MinValue = value; Set(m_Value); UpdateFillSize(); } } }
    public float maxValue { get => m_MaxValue; set { if (m_MaxValue != value) { m_MaxValue = value; Set(m_Value); UpdateFillSize(); } } }
    public bool wholeNumbers { get => m_WholeNumbers; set { if (m_WholeNumbers != value) { m_WholeNumbers = value; Set(m_Value); UpdateFillSize(); } } }

    /// <summary>
    /// Nilai Health Bar saat ini (antara MinValue dan MaxValue).
    /// </summary>
    public virtual float value
    {
        get => wholeNumbers ? Mathf.Round(m_Value) : m_Value;
        set => Set(value);
    }

    /// <summary>
    /// Event yang dipicu saat nilai Health Bar berubah.
    /// </summary>
    public HealthBarEvent onValueChanged { get => m_OnValueChanged; set => m_OnValueChanged = value; }

    /// <summary>
    /// Nilai Health Bar yang dinormalisasi (0 hingga 1).
    /// KARENA MIN=0 DAN MAX=1, normalizedValue AKAN SAMA DENGAN value.
    /// </summary>
    public float normalizedValue
    {
        get
        {
            if (Mathf.Approximately(m_MinValue, m_MaxValue)) return 0f;
            return Mathf.InverseLerp(m_MinValue, m_MaxValue, value);
        }
        set => this.value = Mathf.Lerp(m_MinValue, m_MaxValue, value);
    }

    // --- Metode Lifecycle ---

    protected void Awake()
    {
        // Panggil inisialisasi untuk setup awal
        SetupReferences();
    }

    protected void OnEnable()
    {
        // Pastikan visual diperbarui saat aktif
        UpdateLayout();
        UpdateFillSize();
        // Atur nilai tanpa notifikasi di OnEnable
        Set(m_Value, false);
    }

    protected void Update()
    {
        // Hanya untuk mode ExecuteAlways/Editor, pastikan visual diperbarui saat properti diubah
        if (!Application.isPlaying)
        {
            SetupReferences();
            UpdateLayout();
            UpdateFillSize();
        }
    }

#if UNITY_EDITOR
    protected void OnValidate()
    {
        if (m_WholeNumbers)
        {
            m_MinValue = Mathf.Round(m_MinValue);
            m_MaxValue = Mathf.Round(m_MaxValue);
        }

        // Memastikan referensi disiapkan di Editor
        SetupReferences();

        // Panggil Set untuk mengclamp nilai
        Set(m_Value, false);

        // Perbarui visual
        UpdateLayout();
        UpdateFillSize();
    }
#endif

    // --- Metode Inti ---

    /// <summary>
    /// Menyiapkan referensi SpriteRenderer dan ukuran asli.
    /// </summary>
    private void SetupReferences()
    {
        if (m_FillObject == null) return;

        // Dapatkan SpriteRenderer dari Fill Object
        if (m_FillRenderer == null)
        {
            m_FillRenderer = m_FillObject.GetComponent<SpriteRenderer>();
        }

        if (m_FillRenderer == null)
        {
            Debug.LogError("Fill Object harus memiliki komponen SpriteRenderer!");
            return;
        }

        // Catatan: UpdateLayout harus dipanggil sebelum SetupReferences untuk memastikan m_IsHorizontal benar.
        // Panggil UpdateLayout secara eksplisit di sini.
        UpdateLayout();

        // Simpan ukuran asli pada sumbu yang relevan
        if (m_IsHorizontal)
        {
            m_OriginalSize = m_FillObject.transform.localScale.x;
        }
        else
        {
            m_OriginalSize = m_FillObject.transform.localScale.y;
        }
    }

    /// <summary>
    /// Menentukan apakah bar ini horizontal atau vertikal dan apakah terbalik.
    /// </summary>
    private void UpdateLayout()
    {
        m_IsHorizontal = m_Direction == Direction.LeftToRight || m_Direction == Direction.RightToLeft;
    }

    /// <summary>
    /// Mengatur nilai Health Bar.
    /// </summary>
    /// <param name="input">Nilai baru.</param>
    /// <param name="sendCallback">Apakah OnValueChanged harus dipicu.</param>
    public virtual void Set(float input, bool sendCallback = true)
    {
        // Clamp the input
        float newValue = ClampValue(input);

        // Jika nilai tidak berubah, keluar
        if (m_Value == newValue) return;

        m_Value = newValue;
        UpdateFillSize(); // Perbarui visual

        if (sendCallback)
        {
            m_OnValueChanged.Invoke(newValue);
        }
    }

    /// <summary>
    /// Mengclamp nilai menjadi antara MinValue dan MaxValue, dan membulatkan jika wholeNumbers true.
    /// </summary>
    private float ClampValue(float input)
    {
        float newValue = Mathf.Clamp(input, m_MinValue, m_MaxValue);
        if (m_WholeNumbers)
        {
            newValue = Mathf.Round(newValue);
        }
        return newValue;
    }

    /// <summary>
    /// Metode inti: Mengubah ukuran (scale) SpriteRenderer berdasarkan nilai normalisasi.
    /// </summary>
    private void UpdateFillSize()
    {
        if (m_FillObject == null || m_FillRenderer == null)
        {
            SetupReferences(); // Coba setup ulang jika hilang
            if (m_FillObject == null || m_FillRenderer == null) return;
        }

        // Dapatkan skala awal
        Vector3 newScale = m_FillObject.transform.localScale;

        // Nilai normalisasi (0 hingga 1)
        float fillRatio = normalizedValue;

        // Posisi (Offset) untuk memastikan bar mengisi dari arah yang benar
        Vector3 newPosition = m_FillObject.transform.localPosition;
        float halfOriginalSize = 0f;

        if (m_IsHorizontal)
        {
            // Update Skala X
            newScale.x = m_OriginalSize * fillRatio;

            // Hitung offset berdasarkan posisi lokal
            halfOriginalSize = m_OriginalSize / 2f;
            float currentHalfSize = newScale.x / 2f;
            float offset = halfOriginalSize - currentHalfSize;

            switch (m_Direction)
            {
                case Direction.LeftToRight:
                    // Geser ke kiri
                    newPosition.x = -offset;
                    break;
                case Direction.RightToLeft:
                    // Geser ke kanan
                    newPosition.x = offset;
                    break;
            }
        }
        else // Vertikal
        {
            // Update Skala Y
            newScale.y = m_OriginalSize * fillRatio;

            // Hitung offset berdasarkan posisi lokal
            halfOriginalSize = m_OriginalSize / 2f;
            float currentHalfSize = newScale.y / 2f;
            float offset = halfOriginalSize - currentHalfSize;

            switch (m_Direction)
            {
                case Direction.BottomToTop:
                    // Geser ke bawah
                    newPosition.y = -offset;
                    break;
                case Direction.TopToBottom:
                    // Geser ke atas
                    newPosition.y = offset;
                    break;
            }
        }

        // Terapkan perubahan
        m_FillObject.transform.localScale = newScale;
        m_FillObject.transform.localPosition = newPosition;
    }
}
