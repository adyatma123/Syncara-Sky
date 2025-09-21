using UnityEngine;
using UnityEngine.UI; // Required for UI elements like Slider or Image

/// <summary>
/// Manages a 3D health bar's position above the player and updates its fill based on aircraft health.
/// This script should be attached to a World Space Canvas containing the health bar UI.
/// </summary>
public class PlayerHealthBar : MonoBehaviour
{
    [Header("Target & Offset")]
    [Tooltip("The tag of the aircraft GameObject. Ensure your aircraft has this tag (e.g., 'Player').")]
    public string aircraftTag = "Player"; // Tag to find the aircraft GameObject
    [Tooltip("The vertical offset from the aircraft's position to place the health bar.")]
    public Vector3 offset = new Vector3(0, 2.5f, 0); // Default offset above the aircraft
    [Tooltip("The speed at which the health bar smoothly follows the aircraft's position.")]
    public float followSpeed = 5f; // Speed for smooth position following, units per second

    [Header("Health Bar UI References")]
    [Tooltip("The UI Slider component that represents the health bar fill.")]
    public Slider healthSlider; // Reference to the UI Slider

    [Header("Fade Settings")]
    [Tooltip("The speed at which the health bar fades in and out.")]
    public float fadeSpeed = 2f;
    [Tooltip("The time (in seconds) the health bar stays visible after taking damage.")]
    public float fadeDelay = 3f;

    private GameObject aircraftGameObject;
    private AircraftController aircraftController; // Reference to the AircraftController script
    private CanvasGroup canvasGroup;
    private float fadeTimer;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Used to find the aircraft and get its controller component.
    /// </summary>
    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("PlayerHealthBar requires a CanvasGroup component on the same GameObject.", this);
            enabled = false;
        }

        // Set the initial alpha to 0 (invisible)
        canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Finds the aircraft GameObject by tag and attempts to get its AircraftController.
    /// </summary>
    private void FindAircraftAndController()
    {
        aircraftGameObject = GameObject.FindGameObjectWithTag(aircraftTag);
        if (aircraftGameObject != null)
        {
            aircraftController = aircraftGameObject.GetComponent<AircraftController>();
            if (aircraftController != null && healthSlider != null)
            {
                // Initialize the slider's max value to aircraft's max health
                healthSlider.maxValue = aircraftController.maxHealth;
                // Set initial health value to the current health of the aircraft
                healthSlider.value = aircraftController.currentHealth;
                Debug.Log("Aircraft 3D Health Bar initialized successfully.");
            }
            else
            {
                if (aircraftController == null)
                    Debug.LogError($"AircraftController not found on GameObject with tag '{aircraftTag}'. Health bar cannot function! Ensure player has AircraftController.", this);
                if (healthSlider == null)
                    Debug.LogError("Health Slider UI component not assigned! Please assign it in the Inspector.", this);
                enabled = false; // Disable script if essential components are missing
            }
        }
    }

    /// <summary>
    /// Called once per frame. Updates the health bar's position and fill amount.
    /// </summary>
    void Update()
    {
        // Continuously try to find the aircraft if it's not found, in case it spawns later or is re-enabled.
        if (aircraftGameObject == null || aircraftController == null)
        {
            FindAircraftAndController();
            if (aircraftGameObject == null) return; // If still not found, exit this frame's update
        }

        // --- Update health bar position smoothly ---
        Vector3 targetPosition = aircraftGameObject.transform.position + offset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);

        // --- Update health bar fill amount ---
        if (healthSlider != null)
        {
            // Update max value in case max health changes during gameplay (e.g., power-ups)
            if (healthSlider.maxValue != aircraftController.maxHealth)
            {
                healthSlider.maxValue = aircraftController.maxHealth;
            }
            // Always update the current value
            healthSlider.value = aircraftController.currentHealth;
        }

        // --- Optional: Make the health bar always face the camera (Billboard effect) ---
        if (Camera.main != null)
        {
            // Face the camera, but only rotate on Y axis (to prevent tilting with camera pitch)
            // Or, for a full billboard that always looks at the camera:
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                             Camera.main.transform.rotation * Vector3.up);
        }
        else
        {
            Debug.LogWarning("Main Camera not found for health bar billboard effect. Ensure your camera is tagged 'MainCamera'.");
        }

        // --- Manage the fade timer and alpha ---
        if (fadeTimer > 0)
        {
            fadeTimer -= Time.deltaTime;
            // Fade in smoothly when timer starts
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, fadeSpeed * Time.deltaTime);
        }
        else
        {
            // Fade out smoothly when timer is complete
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, fadeSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// Public method to be called by another script (e.g., AircraftController)
    /// when the aircraft takes damage.
    /// </summary>
    public void OnTakeDamage()
    {
        fadeTimer = fadeDelay;
        // Optionally, make it instantly visible on damage
        canvasGroup.alpha = 1f;
    }
}
