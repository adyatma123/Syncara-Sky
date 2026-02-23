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
    private ModularBossController _bossController;

    void Start()
    {
        // Find the main boss controller in the parent
        _bossController = GetComponentInParent<ModularBossController>();

        // Initialize random wave parameters
        _speedX = Random.Range(minSpeedX, maxSpeedX);
        _speedY = Random.Range(minSpeedY, maxSpeedY);
        _phaseX = Random.Range(0f, Mathf.PI * 2f);
        _phaseY = Random.Range(0f, Mathf.PI * 2f);

        _anchorPosition = transform.position;
    }

    void Update()
    {
        // 1. Safety check for the controller
        if (_bossController == null) return;

        // 2. Check if the initial movement is complete and if we are in the correct phase
        if (!IsCorrectPhaseActive())
        {
            // Keep updating the anchor position so that when the phase starts, 
            // the wave begins from the part's current position.
            _anchorPosition = transform.position;
            return;
        }

        ApplyWavyMotion();
    }

    private bool IsCorrectPhaseActive()
    {
        // Accessing variables from the ModularBossController
        // Note: You may need to make 'isInitialMovementComplete' and 'currentPhaseIndex' 
        // public or add public properties in your ModularBossController script.

        bool movementDone = _bossController.isInitialMovementComplete;
        bool phaseMatch = _bossController.currentPhaseIndex == activeDuringPhase;

        return movementDone && phaseMatch;
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