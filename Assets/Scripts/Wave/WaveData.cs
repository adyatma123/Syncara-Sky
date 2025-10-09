using UnityEngine;

/// <summary>
/// Draws a colored wire cube in the Scene view to visualize the boundary/size
/// of the enemy prefab for easier placement and visual checks.
/// </summary>
public class WaveData : MonoBehaviour
{
    [Header("Gizmo Settings")]
    [Tooltip("Toggle to enable/disable the drawing of the Gizmo when the object is selected.")]
    public bool drawBounds = true;

    [Tooltip("The color of the wire cube used for visualization.")]
    public Color gizmoColor = Color.red;

    [Header("Manual Dimensions (World Units)")]
    [Tooltip("The manual size of the boundary on the X-axis (Width).")]
    public float sizeX = 5f;

    [Tooltip("The manual size of the boundary on the Z-axis (Depth).")]
    public float sizeZ = 5f;

    /// <summary>
    /// Draws the Gizmo visualization in the Scene view only when the GameObject is selected.
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!drawBounds) return;

        // Set the color for the Gizmo
        Gizmos.color = gizmoColor;

        // The size vector uses the manually defined X and Z, and a small arbitrary Y for a plane effect.
        // We ensure the size is positive.
        Vector3 size = new Vector3(Mathf.Abs(sizeX), 0.1f, Mathf.Abs(sizeZ));

        // We set the matrix to the object's local space. This makes the Gizmo rotate and scale with the GameObject.
        Gizmos.matrix = transform.localToWorldMatrix;

        // Draw the wire cube at the local origin (Vector3.zero)
        Gizmos.DrawWireCube(Vector3.zero, size);
    }
}
