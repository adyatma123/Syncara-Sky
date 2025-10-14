using UnityEngine;
using System.Collections;

/// <summary>
/// Manages the activation and controlled fade-out of a complex air pressure particle effect.
/// This script tracks the attached aircraft's velocity and triggers the effect when moving backward
/// beyond a defined threshold.
/// </summary>
public class AirPressureFXController : MonoBehaviour
{
    [Header("Velocity Settings")]
    [Tooltip("Reference to the aircraft's Transform for calculating movement.")]
    public Transform aircraftTransform; // MUST be assigned in Inspector
    [Tooltip("The threshold velocity on the X-axis to trigger the effect (e.g., -0.1).")]
    public float backwardVelocityThreshold = -0.1f;

    [Header("Fade Settings")]
    [Tooltip("The time (in seconds) it takes for the effect to fade out.")]
    public float FadeOutDuration = 0.5f;

    [Header("Effect References")]
    [Tooltip("Drag all ParticleSystem components that should be faded into this array.")]
    public ParticleSystem[] effectParticleSystems;

    private Coroutine fadeCoroutine;
    private Vector3 previousPosition;
    private Vector3 currentVelocity;

    /// <summary>
    /// Validates assignments and initializes position tracking.
    /// </summary>
    void Awake()
    {
        if (aircraftTransform == null)
        {
            Debug.LogError($"AirPressureFXController on '{gameObject.name}' is missing a reference to the Aircraft Transform!");
        }

        if (effectParticleSystems == null || effectParticleSystems.Length == 0)
        {
            Debug.LogError($"AirPressureFXController on '{gameObject.name}' has no ParticleSystem references assigned. Please assign them in the Inspector.");
        }
    }

    void Start()
    {
        // Initialize position tracker
        if (aircraftTransform != null)
        {
            previousPosition = aircraftTransform.position;
        }
        else
        {
            // Fallback if aircraftTransform is null
            previousPosition = transform.position;
        }

        // Immediately stop all particle emission on start
        foreach (var ps in effectParticleSystems)
        {
            if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // IMPORTANT: The parent GameObject remains ACTIVE, only particle systems are stopped.
    }

    /// <summary>
    /// Tracks velocity and controls the effect visibility based on backward movement.
    /// </summary>
    void Update()
    {
        if (aircraftTransform == null) return;

        // 1. Calculate velocity
        currentVelocity = (aircraftTransform.position - previousPosition) / Time.deltaTime;
        previousPosition = aircraftTransform.position;

        // 2. Control the effect based on X velocity
        UpdateEffectVisibility();
    }

    /// <summary>
    /// Reads the X-axis velocity and activates/deactivates the air pressure effect with a fade.
    /// </summary>
    private void UpdateEffectVisibility()
    {
        if (effectParticleSystems == null || effectParticleSystems.Length == 0) return;

        bool isMovingBackward = currentVelocity.x < backwardVelocityThreshold;

        if (isMovingBackward)
        {
            // Activation Logic
            ActivateEffect();
        }
        else
        {
            // Deactivation/Fade Logic
            DeactivateEffectWithFade();
        }
    }

    /// <summary>
    /// Instantly activates particle emission and ensures all particle systems have full opacity.
    /// Stops any ongoing fade-out.
    /// </summary>
    public void ActivateEffect()
    {
        if (effectParticleSystems == null || effectParticleSystems.Length == 0) return;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        // Play the particle system and set full opacity
        foreach (var ps in effectParticleSystems)
        {
            if (ps == null) continue; // Skip if a slot is empty

            // Start emitting new particles
            if (!ps.isPlaying)
            {
                ps.Play();
            }

            // Instantly set full opacity on the main start color
            var main = ps.main;
            // Note: We use the existing color components and set alpha back to 1 (full opacity)
            main.startColor = new Color(main.startColor.color.r, main.startColor.color.g, main.startColor.color.b, 1f);
        }
    }

    /// <summary>
    /// Starts the smooth fade-out process (stop emission and fade existing particles).
    /// </summary>
    public void DeactivateEffectWithFade()
    {
        // Stop the emission immediately, but let existing particles fade out via the coroutine
        foreach (var ps in effectParticleSystems)
        {
            if (ps == null) continue;
            if (ps.isPlaying)
            {
                // Stop emission (will continue to simulate existing particles)
                ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        // Start the color fade coroutine if not already running
        if (fadeCoroutine == null)
        {
            fadeCoroutine = StartCoroutine(FadeOutEffect());
        }
    }

    /// <summary>
    /// Coroutine to fade out all particle systems' start color over a set duration.
    /// </summary>
    IEnumerator FadeOutEffect()
    {
        float timer = 0f;

        // Store the initial colors of all particle systems before starting the fade
        Color[] startColors = new Color[effectParticleSystems.Length];
        for (int i = 0; i < effectParticleSystems.Length; i++)
        {
            // Store color only if reference is valid
            if (effectParticleSystems[i] != null)
            {
                startColors[i] = effectParticleSystems[i].main.startColor.color;
            }
        }

        while (timer < FadeOutDuration)
        {
            timer += Time.deltaTime;
            float t = timer / FadeOutDuration;
            // Calculate interpolation factor for the alpha fade (from 1 to 0)
            float fadeFactor = Mathf.Lerp(1f, 0f, t);

            for (int i = 0; i < effectParticleSystems.Length; i++)
            {
                var ps = effectParticleSystems[i];
                if (ps == null) continue; // Skip if reference is null

                var main = ps.main;
                Color originalColor = startColors[i];

                // Apply the fade factor to the original alpha of the particle system
                main.startColor = new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a * fadeFactor);
            }

            yield return null;
        }

        // Final cleanup after fade is complete
        foreach (var ps in effectParticleSystems)
        {
            if (ps == null) continue; // Skip if reference is null
            var main = ps.main;
            Color originalColor = main.startColor.color;
            // Ensure final alpha is 0
            main.startColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);

            // Stop the remaining particles completely
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // Reset coroutine variable
        fadeCoroutine = null;
    }
}
