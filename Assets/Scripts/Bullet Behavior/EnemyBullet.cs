using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public EnemyProps Enemyprops;
    public AircraftController player;

    public int damage = 10;
    public float bulletSpeed = 200f;
    private Vector3 moveDirection;

    public GameObject owner;

    void Update()
    {
        transform.Translate(Vector3.forward * bulletSpeed * Time.deltaTime * -1);
        // Check if the bullet is outside the camera's view
        Vector3 viewportPosition = Camera.main.WorldToViewportPoint(transform.position);
        if (viewportPosition.x < 0 || viewportPosition.x > 1 || viewportPosition.y < 0 || viewportPosition.y > 1 || viewportPosition.z < 0)
        {
            Destroy(gameObject);
        }
    }

    public void SetDirectionAndSpeed(Vector3 direction, float speed)
     {
        moveDirection = direction.normalized; // Ensure direction is normalized
        bulletSpeed = speed;
     }

    void OnCollisionEnter(Collision collision)
    {
        // Get the EnemyProps component (assuming it exists)
        AircraftController player = collision.gameObject.GetComponent<AircraftController>();

        if (collision.gameObject == owner)
        {
            // If the collided object is the owner, ignore this collision.
            return;
        }

        if (collision.gameObject.CompareTag("Enemy"))
        {
            // If the collided object is an Enemy, do nothing and return.
            // The bullet passes through enemies.
            return;
        }

        if (collision.gameObject.CompareTag("Player"))
        {
            if (player != null)
            {
                if (VisualEffectManager.Instance != null && !string.IsNullOrEmpty("Bullet Impact"))
                {
                    // Spawn the effect at the enemy's position and current rotation
                    VisualEffectManager.Instance.PlayEffect("Bullet Impact", transform.position, transform.rotation);
                }

                SoundManager.Instance.PlaySFX("Player Hit");

                player.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
    }
}