using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Aimbot : MonoBehaviour
{
    [Header("Aim Settings")]
    public float rotationSpeed = 5f;

    [Tooltip("Controls the strength of predictive aiming (0 = off, 1 = full prediction).")]
    [Range(0f, 1f)]
    public float predictiveAimSensitivity = 1f;

    [Tooltip("Adds or subtracts distance along the direction of the prediction vector. Positive values aim further ahead.")]
    public float predictiveDistanceOffset = 0f;

    // Public field for the Gun script to set the actual bullet speed
    [HideInInspector] // Hide in Inspector since it is set programmatically
    public float CurrentBulletSpeed = 0f;

    [Header("Targeting Settings")]
    public float lockRadius = 120f;

    private Transform nearestEnemy;
    private Rigidbody nearestEnemyRb; // Cache Rigidbody for velocity access
    public bool showTargetFollowRadiusGizmo = true;

    void Update()
    {
        FindNearestEnemy();

        // Rotate towards the nearest enemy or reset rotation
        if (nearestEnemy != null)
        {
            Vector3 targetPosition = nearestEnemy.position;
            Vector3 finalAimPosition;

            // Check if predictive aiming is enabled and the enemy has a Rigidbody
            // Use CurrentBulletSpeed > 0 check as a safety for the division
            if (predictiveAimSensitivity > 0f && nearestEnemyRb != null && CurrentBulletSpeed > 0f)
            {
                // Get the target's velocity
                Vector3 targetVelocity = nearestEnemyRb.velocity;

                // 1. Calculate the full predicted position
                Vector3 predictedPosition = AimPrediction.GetPredictedLeadPosition(
                    targetPosition,
                    targetVelocity,
                    transform.position,
                    CurrentBulletSpeed
                );

                // 2. Interpolate based on sensitivity
                Vector3 interpolatedPosition = Vector3.Lerp(targetPosition, predictedPosition, predictiveAimSensitivity);

                // --- Apply the Predictive Distance Offset ---
                if (predictiveDistanceOffset != 0f)
                {
                    // The 'lead direction' is from the shooter to the interpolated target
                    Vector3 leadDirection = (interpolatedPosition - transform.position).normalized;

                    // Apply the offset along that direction vector
                    finalAimPosition = interpolatedPosition + (leadDirection * predictiveDistanceOffset);
                }
                else
                {
                    finalAimPosition = interpolatedPosition;
                }
            }
            else
            {
                // Prediction off, or target/speed invalid, aim directly at the current position
                finalAimPosition = targetPosition;
            }

            // Calculate the direction vector using the final determined aim position
            Vector3 direction = (finalAimPosition - transform.position).normalized;

            // Rotate the Aimbot
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            // Reset rotation to default (e.g., facing forward)
            transform.rotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// Searches for the nearest *alive* enemy within the lock radius and stores its Transform and Rigidbody.
    /// </summary>
    void FindNearestEnemy()
    {
        nearestEnemy = null;
        nearestEnemyRb = null; // Clear the Rigidbody reference

        Collider[] colliders = Physics.OverlapSphere(transform.position, lockRadius);
        float nearestDistance = Mathf.Infinity;

        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Enemy"))
            {
                AfterDeathAnimation deathAnim = collider.GetComponent<AfterDeathAnimation>();


                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = collider.transform;
                    // Attempt to get the Rigidbody here
                    nearestEnemyRb = collider.GetComponent<Rigidbody>();


                    // NEW CHECK: Check if the AfterDeathAnimation component exists AND if its IsDead property is true.
                    if (deathAnim != null && deathAnim.IsDead)
                    {
                        continue; // Skip this enemy, it is already dead.
                    }
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (showTargetFollowRadiusGizmo)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, lockRadius);
        }

        // Optional: Draw a line to the predicted target position for debugging
        if (nearestEnemy != null && nearestEnemyRb != null && predictiveAimSensitivity > 0f)
        {
            // Calculate predicted position for drawing
            Vector3 predictedPosition = AimPrediction.GetPredictedLeadPosition(
               nearestEnemy.position,
               nearestEnemyRb.velocity,
               transform.position,
               CurrentBulletSpeed
           );

            // Calculate the final interpolated position
            Vector3 interpolatedPosition = Vector3.Lerp(nearestEnemy.position, predictedPosition, predictiveAimSensitivity);

            // Apply offset for gizmo drawing consistency
            Vector3 leadDirection = (interpolatedPosition - transform.position).normalized;
            Vector3 finalAimPosition = interpolatedPosition + (leadDirection * predictiveDistanceOffset); // Applying offset to Gizmo drawing

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(finalAimPosition, 1f); // Red sphere at the final aim spot
            Gizmos.DrawLine(transform.position, finalAimPosition);
        }
    }
}