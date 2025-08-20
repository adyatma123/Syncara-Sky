using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Patrol : MonoBehaviour
{
    private bool movingRight = true; // Tracks the current direction of movement

    public int patrolSpeed = 20;

    /// <summary>
    /// Makes the enemy patrol horizontally within the camera's view.
    /// The enemy will move right until it hits the right edge of the camera,
    /// then move left until it hits the left edge, and so on.
    /// </summary>
    /// <param name="patrolSpeed">The speed at which the enemy patrols.</param>
    public void PatrolMovement(float patrolSpeed)
    {
        // Ensure there's a main camera in the scene
        if (Camera.main == null)
        {
            Debug.LogError("Main Camera is null. Please ensure you have a Camera tagged as 'MainCamera' in your scene.");
            return; // Exit if no camera is found to prevent errors
        }

        // Calculate the world coordinates of the left and right edges of the camera's viewport
        // The Z-position of the object is used to get the correct world point at the object's depth.
        float minX = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, transform.position.z - Camera.main.transform.position.z)).x;
        float maxX = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, transform.position.z - Camera.main.transform.position.z)).x;

        // Move the enemy based on the current direction
        if (movingRight)
        {
            // Move right
            transform.position += Vector3.right * patrolSpeed * Time.deltaTime;

            // If the enemy moves beyond the right edge, reverse direction
            if (transform.position.x >= maxX)
            {
                movingRight = false;
            }
        }
        else
        {
            // Move left
            transform.position -= Vector3.right * patrolSpeed * Time.deltaTime;

            // If the enemy moves beyond the left edge, reverse direction
            if (transform.position.x <= minX)
            {
                movingRight = true;
            }
        }
    }
}
