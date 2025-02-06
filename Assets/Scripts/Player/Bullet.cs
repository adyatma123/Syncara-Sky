using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Gun gun;
    public EnemyProps enemy;

    public int damage = 100;

    void Update()
    {
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
}