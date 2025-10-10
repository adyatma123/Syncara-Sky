using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Gun gun;

    public int damage = 100;
    public int bulletSpeed = 50;

    void Update()
    {
        // NOTE: If the bullet is spawned facing forward (World Z), this movement is backwards.
        // It should usually be: transform.Translate(Vector3.forward * bulletSpeed * Time.deltaTime);
        // However, maintaining the original direction:
        transform.Translate(Vector3.forward * bulletSpeed * Time.deltaTime * -1);

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

        // 1. Ignore collision with the Player vehicle itself.
        if (collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        // 2. Check if the object is an Enemy
        if (collision.gameObject.CompareTag("Enemy"))
        {
            if (enemy != null)
            {
                // CRITICAL FIX: Pass the bullet's damage and the bullet's GameObject (this.gameObject)
                // as the damage source. The EnemyProps script will check the source's tag ("PlayerProjectile").
                enemy.TakeDamage(damage, this.gameObject);
                Destroy(gameObject);
            }
        }
    }
}
