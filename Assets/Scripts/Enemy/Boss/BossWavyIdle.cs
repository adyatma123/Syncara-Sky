using UnityEngine;

/// <summary>
/// Provides a wavy motion that only activates during a specific boss phase
/// after the initial movement is finished.
/// </summary>
public class BossWavyIdle : MonoBehaviour, IEnemyBehavior
{
    [Header("Phase Activation")]
    [Tooltip("The phase index (0, 1, 2...) during which this movement should be active.")]
    public int activeDuringPhase = 0;

    [Header("Horizontal Wave (X)")]
    public float maxRangeX = 2.0f;
    public float minSpeedX = 0.5f;
    public float maxSpeedX = 1.5f;

    [Header("Vertical Wave (Y)")]
    public float maxRangeY = 1.0f;
    public float minSpeedY = 0.3f;
    public float maxSpeedY = 1.2f;

    // State tracking
    private Vector3 _anchorPosition;
    private float _speedX, _speedY;
    private float _phaseX, _phaseY;

    // References
    private BossPhaseController _bossController;

    void Start()
    {
        // Find the main boss controller in the parent
        _bossController = GetComponentInParent<BossPhaseController>();

        // Initialize random wave parameters
        _speedX = Random.Range(minSpeedX, maxSpeedX);
        _speedY = Random.Range(minSpeedY, maxSpeedY);
        _phaseX = Random.Range(0f, Mathf.PI * 2f);
        _phaseY = Random.Range(0f, Mathf.PI * 2f);

        _anchorPosition = transform.position;
    }

    private bool wasActive = false;

    void Update()
    {
        if (_bossController == null) return;

        bool isActive = IsCorrectPhaseActive();

        if (!isActive)
        {
            wasActive = false;
            return;
        }

        // 🔥 Set anchor ONLY when phase starts
        if (!wasActive)
        {
            _anchorPosition = transform.position;
            wasActive = true;
        }

        ApplyWavyMotion();
    }

    private bool IsCorrectPhaseActive()
    {
        if (_bossController == null) return false;

        return _bossController.isInitialMovementComplete &&
               _bossController.CurrentPhase == activeDuringPhase;
    }

    private void ApplyWavyMotion()
    {
        float xOffset = Mathf.Sin(Time.time * _speedX + _phaseX) * maxRangeX;
        float yOffset = Mathf.Cos(Time.time * _speedY + _phaseY) * maxRangeY;

        transform.position = new Vector3(
            _anchorPosition.x + xOffset,
            _anchorPosition.y + yOffset,
            _anchorPosition.z
        );
    }
}