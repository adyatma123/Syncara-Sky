using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Aimbot : MonoBehaviour
{
    public float searchRadius = 10f;
    public float rotationSpeed = 5f;

    private Transform nearestEnemy;
    public bool showTargetFollowRadiusGizmo = true;

    void Update()
    {
        // Find the nearest enemy within the search radius
        Collider[] colliders = Physics.OverlapSphere(transform.position, searchRadius);
        float nearestDistance = Mathf.Infinity;

        foreach (Collider collider in colliders)
        {
            if (collider.tag == "Enemy")
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = collider.transform;
                }
            }
        }

        // Rotate towards the nearest enemy or reset rotation
        if (nearestEnemy != null)
        {
            Vector3 direction = (nearestEnemy.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        else
        {
            // Reset rotation to default (e.g., facing forward)
            transform.rotation = Quaternion.identity;
        }
    }

    private void OnDrawGizmos()
    {
        if (showTargetFollowRadiusGizmo)
        {
            Gizmos.color = Color.green; // Use a clear green color for better visibility
            Gizmos.DrawWireSphere(transform.position, searchRadius);
        }
    }
}