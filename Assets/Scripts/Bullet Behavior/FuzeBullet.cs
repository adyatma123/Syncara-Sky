using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FuzeBullet : MonoBehaviour
{
    public Gun gun;
    public EnemyProps enemy;
    public float proximityRadius = 10f; // Radius to detect enemies
    public float selfDestructDelay = 0.1f; // Time before explosion after proximity
    public int damage;
    public GameObject explosionEffect; // Prefab for the explosion effect

    private bool hasDetectedEnemy = false;
    private float selfDestructTimer = 0f; // Timer for self-destruction

    void Update()
    {
        // Check if the bullet is outside the camera's view
        Vector3 viewportPosition = Camera.main.WorldToViewportPoint(transform.position);
        if (viewportPosition.x < 0 || viewportPosition.x > 1 || viewportPosition.y < 0 || viewportPosition.y > 1 || viewportPosition.z < 0)
        {
            Destroy(gameObject);
        }

        // Check for enemies within proximity radius (only if not already triggered)
        if (!hasDetectedEnemy)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, proximityRadius);
            foreach (Collider collider in colliders)
            {
                if (collider.CompareTag("Enemy"))
                {
                    hasDetectedEnemy = true;
                    enemy = collider.GetComponent<EnemyProps>(); // Get the enemy component
                    break; // Exit the loop once an enemy is found
                }
            }
        }

        if (hasDetectedEnemy)
        {
             Explode();
        }
        else // If no enemy detected, start self-destruct timer
        {
            selfDestructTimer += Time.deltaTime;
            if (selfDestructTimer >= selfDestructDelay)
            {
                Explode();
            }
        }
    }

    void Explode()
    {
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        // Instantiate explosion effect
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, transform.rotation);
        }

        Destroy(gameObject); // Destroy the bullet
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, proximityRadius);
    }

   /* void OnCollisionEnter(Collision collision)
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
    }*/
}