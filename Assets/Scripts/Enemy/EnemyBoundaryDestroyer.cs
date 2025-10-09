using UnityEngine;

/// <summary>
/// Handles the destruction of an enemy GameObject by checking if it has passed 
/// a specific world boundary (the 'lower viewpoint'), but only AFTER it has 
/// been visible to the player's camera at least once.
/// 
/// REQUIRES a Renderer component on the GameObject or a child.
/// </summary>
public class EnemyBoundaryDestroyer : MonoBehaviour
{
    [Tooltip("The World Z position where the enemy will be destroyed (e.g., -10 for off-screen bottom).")]
    public float destroyBoundaryZ = -1f;

    // Flag to track whether the enemy has entered the player's camera view at least once.
    private bool hasBeenVisible = false;

    // References to other components
    private AIShoot aiShoot;

    void Start()
    {
        // Get AI Shoot reference to ensure we can stop it if the game object leaves the boundary 
        // in a way that OnBecameInvisible is missed (e.g. if the camera gets disabled/destroyed)
        aiShoot = GetComponent<AIShoot>();
    }

    /// <summary>
    /// Called by the Renderer system when the object is visible by any camera.
    /// Sets the flag, making the enemy eligible for destruction later.
    /// </summary>
    void OnBecameVisible()
    {
        // Set the flag once the enemy enters the view.
        if (!hasBeenVisible)
        {
            hasBeenVisible = true;
            Debug.Log($"[{gameObject.name}] entered the screen. Destruction eligibility enabled.");
            // NOTE: AI Shoot component should handle its own activation here.
        }
    }

    /// <summary>
    /// This method is now empty in this script. AIShoot will use OnBecameInvisible 
    /// for deactivation, while the destruction boundary is checked in Update().
    /// </summary>
    void OnBecameInvisible()
    {
        // This is necessary if you rely on AIShoot to stop shooting when it leaves the sides/top.
        // We ensure that if the enemy leaves the view *before* crossing the bottom boundary, 
        // the AI still stops firing.
        if (aiShoot != null)
        {
            aiShoot.Deactivate();
        }
    }

    void Update()
    {
        // Only check for boundary destruction if the enemy has been visible.
        if (hasBeenVisible)
        {
            // Check if the enemy has moved beyond the lower viewpoint boundary on the Z-axis.
            // NOTE: Assuming positive Z is 'up'/'forward' and negative Z is 'down'/'back'.
            if (transform.position.z < destroyBoundaryZ)
            {
                Debug.Log($"[{gameObject.name}] passed Z boundary ({destroyBoundaryZ}). Destroying object.");

                // Ensure AI shoot is explicitly stopped before destruction
                if (aiShoot != null)
                {
                    aiShoot.Deactivate();
                }

                // Perform the destruction
                Destroy(gameObject);
            }
        }
    }
}
