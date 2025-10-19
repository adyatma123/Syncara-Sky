using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public Gun gun;

    [Header("Bullet Stats")]
    public int damage = 100;
    public float bulletSpeed = 50f; // Changed to float for consistency with Time.deltaTime

    [Header("Bouncing Settings")]
    [Tooltip("The percentage chance (0 to 1) this bullet will bounce on hitting an ENEMY or other object.")]
    [Range(0f, 1f)]
    public float bounceChance = 0.5f; // 50% chance to bounce

    [Tooltip("The maximum angle range (+/-) the bounced bullet will deviate from the perfect reflection.")]
    [Range(0f, 45f)]
    public float bounceAngleMaxRange = 15f;

    private Rigidbody rb;
    private int bounceCount = 0;

    void Start()
    {
        // Add Rigidbody setup for proper physics interaction if not already present
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            // Add a kinematic Rigidbody if one doesn't exist to ensure OnCollisionEnter is reliable
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    void Update()
    {
        // Maintaining your current movement style (moving backwards along transform.forward)
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
        if (collision.gameObject.CompareTag("Enemy") && enemy != null)
        {
            // Try to bounce off the enemy. The function will return true if it bounced.
            if (!TryBounce(collision, applyDamage: true))
            {
                // If the bounce failed (or bounce chance was zero), handle damage and destroy.
                HandleEnemyDestruction(collision, enemy);
            }
        }
        else
        {
            // === PENAMBAHAN KODE UNTUK MENGABAIKAN OBJEK YANG TIDAK BER-TAG ===
            if (collision.gameObject.CompareTag("Untagged"))
            {
                return; // Abaikan objek yang tidak memiliki tag (secara default "Untagged")
            }
            // ==================================================================

            // Jika memiliki tag selain "Player" dan "Enemy" (misalnya "Wall", "Ground")
            // dan BUKAN "Untagged", maka coba memantul
            TryBounce(collision, applyDamage: false);
        }
    }

    /// <summary>
    /// Deals damage to the enemy and destroys the bullet. Called when bounce chance fails.
    /// </summary>
    private void HandleEnemyDestruction(Collision collision, EnemyProps enemy)
    {
        // Play effects and sound
        if (VisualEffectManager.Instance != null && !string.IsNullOrEmpty("Bullet Impact"))
        {
            VisualEffectManager.Instance.PlayEffect("Bullet Impact", transform.position, transform.rotation);
        }
        SoundManager.Instance.PlaySFX("Bullet Impact");

        // Deal damage and destroy the bullet
        enemy.TakeDamage(damage, this.gameObject);
        Destroy(gameObject);
    }

    /// <summary>
    /// Checks the bounce chance and redirects the bullet if successful.
    /// </summary>
    /// <param name="applyDamage">If true, damage is applied to the enemy before bouncing.</param>
    /// <returns>True if the bullet successfully bounced, false otherwise.</returns>
    private bool TryBounce(Collision collision, bool applyDamage)
    {
        // Check if the bounce attempt is successful
        if (Random.value < bounceChance)
        {
            if (applyDamage)
            {
                // Damage the enemy on bounce
                EnemyProps enemy = collision.gameObject.GetComponent<EnemyProps>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage, this.gameObject);
                }
            }

            // --- Bounce Logic (Reflect only on the X-axis) ---

            Vector3 contactNormal = collision.contacts[0].normal;
            Vector3 incidentDirection = -transform.forward; // Since your update code moves backwards

            // 1. Calculate the perfect reflection direction
            Vector3 reflectedDirection = Vector3.Reflect(incidentDirection, contactNormal);

            // 2. FORCING X-AXIS ONLY REFLECTION:
            // We zero out the Y and Z components of the normal before reflection to simulate a flat, vertical bounce.
            // However, a cleaner way is to keep the incident Z direction and only flip the X direction relative to the world.

            // This isolates the lateral (left/right) reflection while maintaining forward movement.
            Vector3 finalBounceDirection = incidentDirection;

            // If the normal is mostly pointing left/right (along world X), flip the bullet's current X movement
            // This is a simplified way to ensure the bounce is mostly lateral.
            if (Mathf.Abs(contactNormal.x) > 0.5f)
            {
                finalBounceDirection.x *= -1f;
            }
            else
            {
                // If it's a head-on impact (mostly Z normal), we still want the bullet to reflect sideways slightly.
                // We'll use the reflected X component from the perfect reflection to ensure lateral movement.
                finalBounceDirection.x = reflectedDirection.x;
            }

            // --- Apply Random Angular Deviation to the X-component ---

            float randomAngle = Random.Range(-bounceAngleMaxRange, bounceAngleMaxRange);
            // Apply the random angle deviation by rotating around the Y-axis (for horizontal spread)
            Quaternion randomRotation = Quaternion.AngleAxis(randomAngle, Vector3.up);

            // Apply the random rotation to the final direction
            finalBounceDirection = randomRotation * finalBounceDirection;

            // Ensure Y component is zero if you are working in a 2D plane (to prevent vertical bouncing)
            finalBounceDirection.y = 0;

            // Update the bullet's rotation to face the new bounce direction
            transform.rotation = Quaternion.LookRotation(finalBounceDirection.normalized);

            // Increment bounce counter
            bounceCount++;

            SoundManager.Instance.PlaySFX("Bullet Bounce");
            return true; // Bounce succeeded
        }
        else
        {
            // Bounce failed, destroy the bullet
            Destroy(gameObject);
            return false; // Bounce failed
        }
    }
}
