using UnityEngine;

/// <summary>
/// Controls the continuous rotation of a GameObject (like a helicopter blade or rotor) 
/// around a selectable axis (X, Y, or Z).
/// </summary>
public class HeliBladeRot : MonoBehaviour
{
    // Enum to select the desired axis of rotation in the Inspector.
    public enum RotationAxis
    {
        X_Axis,
        Y_Axis,
        Z_Axis
    }

    [Tooltip("The speed of rotation in degrees per second.")]
    public float rotationSpeed = 100f;

    [Tooltip("Select the local axis around which the object will rotate.")]
    public RotationAxis axis = RotationAxis.Y_Axis;

    private Vector3 rotationVector;

    void Start()
    {
        // Initialize the rotation vector based on the selected enum value.
        SetRotationAxis();
    }

    void Update()
    {
        // Rotate the object around the determined vector continuously.
        transform.Rotate(rotationVector * rotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Maps the selected enum value to the corresponding Vector3 axis.
    /// This method can be called in Start or during initialization.
    /// </summary>
    private void SetRotationAxis()
    {
        switch (axis)
        {
            case RotationAxis.X_Axis:
                rotationVector = Vector3.right;
                break;
            case RotationAxis.Y_Axis:
                rotationVector = Vector3.up;
                break;
            case RotationAxis.Z_Axis:
                rotationVector = Vector3.forward;
                break;
        }
    }

    /// <summary>
    /// Helper method called when an inspector value changes (only runs in the Unity Editor).
    /// This ensures the rotation axis is updated immediately if the user changes the dropdown.
    /// </summary>
    private void OnValidate()
    {
        SetRotationAxis();
    }
}
