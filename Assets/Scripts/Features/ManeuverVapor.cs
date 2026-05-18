using UnityEngine;

public class ManeuverVapor : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign all Particle Systems here (e.g., Left Wing, Right Wing, Fuselage).")]
    public ParticleSystem[] vaporEffects;

    [Header("Settings")]
    [Tooltip("How much negative Z velocity is needed to trigger vapor.")]
    public float backwardThreshold = -0.1f;

    private Vector3 lastPosition;
    private ParticleSystem.EmissionModule[] emissionModules;

    void Start()
    {
        // Initialize the array of emission modules for better performance
        if (vaporEffects != null && vaporEffects.Length > 0)
        {
            emissionModules = new ParticleSystem.EmissionModule[vaporEffects.Length];

            for (int i = 0; i < vaporEffects.Length; i++)
            {
                if (vaporEffects[i] != null)
                {
                    emissionModules[i] = vaporEffects[i].emission;
                    emissionModules[i].enabled = false;
                }
            }
        }

        lastPosition = transform.position;
    }

    void Update()
    {
        // 1. Calculate world displacement
        Vector3 displacement = transform.position - lastPosition;

        // 2. Convert to local space to find the 'Forward/Backward' movement
        Vector3 localDisplacement = transform.InverseTransformDirection(displacement);

        // 3. Calculate local Z velocity: $v_z = \frac{\Delta z}{\Delta t}$
        float localZVelocity = localDisplacement.z / Time.deltaTime;

        // 4. Update all particle systems based on velocity
        bool shouldEmit = localZVelocity < backwardThreshold;
        SetAllVaporsActive(shouldEmit);

        lastPosition = transform.position;
    }

    private void SetAllVaporsActive(bool isActive)
    {
        if (emissionModules == null) return;

        for (int i = 0; i < emissionModules.Length; i++)
        {
            // Only update if the state has changed to save resources
            if (emissionModules[i].enabled != isActive)
            {
                emissionModules[i].enabled = isActive;
            }
        }
    }
}