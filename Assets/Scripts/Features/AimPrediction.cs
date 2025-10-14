using UnityEngine;

public static class AimPrediction
{
    /// <summary>
    /// Calculates the predicted position of a moving target required for a lead shot.
    /// This method uses an iterative approximation to solve the prediction problem.
    /// </summary>
    /// <param name="targetPosition">The target's current world position.</param>
    /// <param name="targetVelocity">The target's current world velocity (from its Rigidbody).</param>
    /// <param name="shooterPosition">The position of the gun/shooter.</param>
    /// <param name="bulletSpeed">The speed of the projectile.</param>
    /// <returns>The calculated lead position where the shooter should aim.</returns>
    public static Vector3 GetPredictedLeadPosition(
        Vector3 targetPosition,
        Vector3 targetVelocity,
        Vector3 shooterPosition,
        float bulletSpeed)
    {
        // Safety check to prevent division by zero
        if (bulletSpeed <= 0.001f)
        {
            return targetPosition;
        }

        // 1. Initial guess for the lead position is the current target position
        Vector3 leadPosition = targetPosition;

        // Use an iterative loop for a high-accuracy approximation. 
        // 3-5 iterations are usually enough for smooth, accurate aiming.
        for (int i = 0; i < 4; i++)
        {
            // 2. Calculate the distance between the shooter and the current lead guess
            float distanceToLead = Vector3.Distance(shooterPosition, leadPosition);

            // 3. Calculate the time it will take the bullet to reach that point
            float travelTime = distanceToLead / bulletSpeed;

            // 4. Calculate the target's new position after that travel time
            Vector3 futureTargetPosition = targetPosition + (targetVelocity * travelTime);

            // 5. Update the lead position guess for the next iteration
            leadPosition = futureTargetPosition;
        }

        return leadPosition;
    }
}
