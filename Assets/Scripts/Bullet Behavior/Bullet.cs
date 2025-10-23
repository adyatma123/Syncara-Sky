using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random; // Explicitly use UnityEngine.Random

public class Bullet : MonoBehaviour
{
    public Gun gun;

    [Header("Bullet Stats")]
    public int damage = 100;
    public float bulletSpeed = 50f;

    [Header("Bouncing Settings")]
    [Tooltip("The percentage chance (0 to 1) this bullet will bounce on hitting an ENEMY or other object.")]
    [Range(0f, 1f)]
    public float bounceChance = 0.5f;

    [Tooltip("The maximum angle range (+/-) the bounced bullet will deviate from the perfect reflection.")]
    [Range(0f, 45f)]
    public float bounceAngleMaxRange = 15f;

    // MODIFIKASI: Critical Hit Settings menggantikan Penetration Settings
    [Header("Critical Hit Settings")]
    [Tooltip("The percentage chance (0 to 1) this bullet will inflict double damage (Critical Hit) on an ENEMY.")]
    [Range(0f, 1f)]
    public float criticalChance = 0.0f; // NEW: Peluang Critical Hit

    private Rigidbody rb;
    private int bounceCount = 0;
    // Variabel penetrationCount dihapus karena tidak lagi digunakan.

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
        bool isEnemy = collision.gameObject.CompareTag("Enemy");

        // 1. Ignore collision with the Player vehicle itself.
        if (collision.gameObject.CompareTag("Player"))
        {
            return;
        }

        // 2. Check if the object is an Enemy
        if (isEnemy && enemy != null)
        {
            // Tentukan damage aktual (Normal atau Critical)
            int actualDamage = TryCriticalHit();

            // 3. Aplikasikan damage ke musuh
            enemy.TakeDamage(actualDamage, this.gameObject);

            // 4. Coba pantulkan peluru. Jika gagal, peluru dihancurkan.
            // Damage sudah diaplikasikan, jadi kita tidak perlu mengaplikasikannya lagi di TryBounce.
            if (!TryBounce(collision, applyDamage: false))
            {
                // Jika bounce gagal (atau bounce chance nol), handle penghancuran peluru.
                HandleBulletDestruction();
            }
            // Catatan: Jika TryBounce berhasil (return true), peluru tidak dihancurkan di sini.
        }
        else
        {
            // === PENAMBAHAN KODE UNTUK MENGABAIKAN OBJEK YANG TIDAK BER-TAG ===
            if (collision.gameObject.CompareTag("Untagged"))
            {
                return;
            }
            // ==================================================================

            // Jika bukan musuh, coba memantul tanpa memberikan damage
            if (!TryBounce(collision, applyDamage: false))
            {
                // Jika bounce gagal, hancurkan peluru
                HandleBulletDestruction();
            }
        }
    }

    /// <summary>
    /// MODIFIKASI: Menghitung peluang Critical Hit dan mengembalikan damage aktual.
    /// </summary>
    /// <returns>Nilai damage aktual (damage normal atau damage * 2 jika critical).</returns>
    private int TryCriticalHit()
    {
        // Cek apakah Critical Hit berhasil
        if (Random.value < criticalChance)
        {
            // Play SFX/VFX untuk efek Critical Hit (opsional)
            // VisualEffectManager.Instance.PlayEffect("Critical Hit Effect", transform.position, transform.rotation);
            // SoundManager.Instance.PlaySFX("CriticalHit");

            Debug.Log($"Bullet struck a Critical Hit! Damage: {damage} -> {damage * 2}");
            return damage * 2; // Gandakan damage
        }

        // Normal Hit
        return damage;
    }

    /// <summary>
    /// Deals with bullet destruction after impact (not counting bounce).
    /// </summary>
    private void HandleBulletDestruction()
    {
        // Play effects and sound
        if (VisualEffectManager.Instance != null && !string.IsNullOrEmpty("Bullet Impact"))
        {
            VisualEffectManager.Instance.PlayEffect("Bullet Impact", transform.position, transform.rotation);
        }
        // Pastikan SoundManager.Instance tidak null
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX("Bullet Impact");
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Checks the bounce chance and redirects the bullet if successful.
    /// </summary>
    /// <param name="applyDamage">If true, damage is applied to the enemy before bouncing. (SEKARANG SELALU FALSE KARENA DAMAGE DIHITUNG DI OnCollisionEnter)</param>
    /// <returns>True if the bullet successfully bounced, false otherwise.</returns>
    private bool TryBounce(Collision collision, bool applyDamage) // Parameter applyDamage sekarang diabaikan
    {
        // Check if the bounce attempt is successful
        if (Random.value < bounceChance)
        {
            // --- Bounce Logic (Reflect only on the X-axis) ---

            Vector3 contactNormal = collision.contacts[0].normal;
            Vector3 incidentDirection = -transform.forward;

            // 1. Calculate the perfect reflection direction
            Vector3 reflectedDirection = Vector3.Reflect(incidentDirection, contactNormal);

            // 2. FORCING X-AXIS ONLY REFLECTION:
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
            return true; // Bounce succeeded
        }
        else
        {
            return false; // Bounce failed
        }
    }
}
