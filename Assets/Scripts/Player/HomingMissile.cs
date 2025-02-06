using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomingMissile : MonoBehaviour
{
    public float speed = 1000f;
    public float steer = 5f;
    public float lockRadius = 100f; // Adjust the search radius as needed
    public int Mdamage = 100;
    public float trackingTime = 1f; // Time in seconds before tracking stops
    public float timeSinceTrackingStarted = 0f;
    private bool isTracking = true;

    private Transform nearestEnemy;
    public EnemyProps enemy;
    Rigidbody rb;
    public bool showTargetFollowRadiusGizmo = true;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        rb.velocity = transform.forward * speed * Time.fixedDeltaTime * 50f;

        if (isTracking)
        {
            // Find the nearest enemy within the search radius
            Collider[] colliders = Physics.OverlapSphere(transform.position, lockRadius);
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

            if (nearestEnemy != null)
            {
                if (nearestEnemy != null)

                {

                    // Move towards the enemy
                    transform.position = Vector3.MoveTowards(transform.position, nearestEnemy.position, Time.deltaTime);



                    // Rotate towards the enemy smoothly

                    Vector3 direction = (nearestEnemy.position - transform.position).normalized;
                    Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, steer * Time.deltaTime);



                    // Increment the tracking timer
                    timeSinceTrackingStarted += Time.deltaTime;



                    // Check if tracking time has elapsed
                    if (timeSinceTrackingStarted >= trackingTime)
                    {
                        isTracking = false; // Disable tracking
                        nearestEnemy = null; // Optional: Clear the nearest enemy
                    }
                }

                // Increment the tracking timer
                timeSinceTrackingStarted += Time.deltaTime;

                // Check if tracking time has elapsed
                if (timeSinceTrackingStarted >= trackingTime)
                {
                    isTracking = false; // Disable tracking
                    nearestEnemy = null;    // Optional: Clear the nearest enemy
                }
            }
        }

        // Check if the bullet is outside the camera's view
        Vector3 viewportPosition = Camera.main.WorldToViewportPoint(transform.position);
        if (viewportPosition.x < 0 || viewportPosition.x > 1 || viewportPosition.y < 0 || viewportPosition.y > 1 || viewportPosition.z < 0)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Get the EnemyProps component (assuming it exists)
        EnemyProps enemy = collision.gameObject.GetComponent<EnemyProps>();

        if (collision.gameObject.CompareTag("Enemy"))
        {
            if (enemy != null)
            {
                enemy.TakeDamage(Mdamage);
                Destroy(gameObject);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (showTargetFollowRadiusGizmo)
        {
            Gizmos.color = Color.red; // Use a clear green color for better visibility
            Gizmos.DrawWireSphere(transform.position, lockRadius);
        }
    }
}
