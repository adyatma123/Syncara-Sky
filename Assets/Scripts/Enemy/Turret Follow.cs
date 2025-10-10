using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretFollow : MonoBehaviour
{
    public float rotationSpeed = 10f; // Speed of rotation in degrees per second
    public float shootRange = 10f; // Range within which the object can "shoot" (perform an action)

    private Transform targetTransform; // Store the target's transform

    void Start()
    {
        // Find the target object by tag at the start of the game.
        GameObject targetObject = GameObject.FindGameObjectWithTag("Player");

        if (targetObject == null)
        {
            return; // Exit early if no object with the tag is found.
        }

        targetTransform = targetObject.transform; // Get the transform of the target object.
    }

    void Update()
    {
        if (targetTransform == null)
        {
            return; // Exit if the target is gone.
        }

        // 1. Rotation:

        // Calculate the direction vector towards the target.
        Vector3 direction = targetTransform.position - transform.position;

        // Only rotate on the Y-axis.
        direction.y = 0f;

        // *** KEY CHANGE: Rotate the direction 180 degrees around the Y-axis ***
        direction = Quaternion.AngleAxis(180, Vector3.up) * direction; // Or -180

        // If the target is close enough, calculate the rotation.
        if (direction.magnitude <= shootRange)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 2. "Shooting" (Example Action):

        //// Check if the target is within shoot range.
        //if (direction.magnitude <= shootRange)
        //{
        //    // Perform your "shooting" action here.  This is just an example:
        //    Debug.Log("Target in range! Performing action...");
        //    // You could instantiate a projectile, play a sound, trigger an animation, etc.
        //}
    }
}