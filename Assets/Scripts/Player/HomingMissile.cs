using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HomingMissile : MonoBehaviour
{
    public float speed = 100f;
    public float steer = 1000f;
    public float lockRadius = 100f; // Adjust the search radius as needed
    public int damage = 10;

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
        rb.velocity = transform.forward * speed * Time.fixedDeltaTime * 10f;

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

        Vector3 direction = (nearestEnemy.position - transform.position).normalized;
        float rotationSteer = Vector3.Cross(transform.forward, direction).x;
        rb.angularVelocity = new Vector3(rotationSteer * steer * 10f, 0, 0);

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
                enemy.TakeDamage(damage);
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
