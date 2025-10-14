using UnityEngine;
using System.Collections; // Required for using Coroutines

/// <summary>
/// Manages screen-space camera effects like shake.
/// Attach this script to the camera GameObject you want to affect.
/// </summary>
public class CameraManager : MonoBehaviour
{
    [Header("Shake Control")]
    [Tooltip("Overall strength multiplier for the shake effect.")]
    [Range(0.1f, 2.0f)]
    public float shakeIntensity = 0.75f;

    [Tooltip("Maximum displacement allowed along the X-axis.")]
    [Range(0.0f, 1.0f)]
    public float maxXShake = 0.3f;

    [Tooltip("Maximum displacement allowed along the Y-axis.")]
    [Range(0.0f, 1.0f)]
    public float maxYShake = 0.4f;

    [Tooltip("How long the camera shake effect lasts in seconds.")]
    public float shakeDuration = 0.25f;

    // Internal variables
    private Vector3 _originalPosition;
    private Coroutine _currentShakeCoroutine;

    void Awake()
    {
        // Store the camera's initial local position.
        // The shake will offset the camera from this position.
        _originalPosition = transform.localPosition;
    }

    /// <summary>
    /// PUBLIC API: Initiates the camera shake effect.
    /// Call this from any script when a 'Player' or 'Enemy' tagged object is destroyed.
    /// </summary>
    public void StartShake()
    {
        // 1. Stop any currently running shake coroutine to ensure the effect is fresh.
        if (_currentShakeCoroutine != null)
        {
            StopCoroutine(_currentShakeCoroutine);
            // Optionally, snap the camera back to origin before the new shake starts:
            transform.localPosition = _originalPosition;
        }

        // 2. Start the new shake routine.
        _currentShakeCoroutine = StartCoroutine(ShakeCamera());
    }

    /// <summary>
    /// Coroutine that handles the frame-by-frame movement of the camera.
    /// </summary>
    private IEnumerator ShakeCamera()
    {
        float elapsed = 0.0f;

        while (elapsed < shakeDuration)
        {
            // Calculate how far into the shake duration we are (0.0 to 1.0)
            float percentComplete = elapsed / shakeDuration;

            // Calculate falloff (1.0 at start, fading to 0.0 at end)
            float falloff = 1.0f - percentComplete;

            // Generate random X and Y offsets, scaled by max limits, overall intensity, and falloff.
            float xOffset = Random.Range(-1f, 1f) * maxXShake * shakeIntensity * falloff;
            float yOffset = Random.Range(-1f, 1f) * maxYShake * shakeIntensity * falloff;

            // Apply the offset to the camera's local position (Z remains constant)
            transform.localPosition = new Vector3(
                _originalPosition.x + xOffset,
                _originalPosition.y + yOffset,
                _originalPosition.z
            );

            elapsed += Time.deltaTime; // Advance the timer
            yield return null;         // Wait for the next frame
        }

        // Final Cleanup: Ensure the camera is exactly at its original position when the shake ends.
        transform.localPosition = _originalPosition;
        _currentShakeCoroutine = null;
    }
}
