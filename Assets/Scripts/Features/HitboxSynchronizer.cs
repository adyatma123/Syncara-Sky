using UnityEngine;

public class HitboxSynchronizer : MonoBehaviour
{
    public Transform modelTransform; // The 3D model's main transform
    public Rigidbody2D hitboxRigidbody; // The child's Rigidbody2D

    void FixedUpdate()
    {
        // Get the 3D position (X and Z coordinates for the ground plane)
        Vector3 modelPos = modelTransform.position;

        // Map the 3D position to a 2D position (X -> X, Z -> Y)
        Vector2 newHitboxPos = new Vector2(modelPos.x, modelPos.z);

        // Use the Rigidbody2D to move the hitbox for proper collision detection
        hitboxRigidbody.position = newHitboxPos;
    }
}