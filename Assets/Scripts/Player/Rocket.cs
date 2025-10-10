using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the movement, damage, and lifetime of an unguided player-fired rocket.
/// Properties are set by the PayloadManager upon instantiation.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Rocket : MonoBehaviour
{
    // Private fields to hold synchronized properties
    private float _speed;
    private int _damage;
    private float _lifeTime;

    // Components
    private Rigidbody rb;
    public ParticleSystem missileBurn;

    // Initial State references for straight flight
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // --- VALIDATION AND INITIALIZATION ---
        if (rb == null)
        {
            Debug.LogError("Rigidbody missing on Rocket. Cannot fly.");
            enabled = false;
            return;
        }

        // Ensure Rigidbody settings are correct for missile flight
        rb.isKinematic = false;
        rb.useGravity = false;

        // Record initial position/rotation, assuming they were set by the FirePoint
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        // Set initial velocity in Start after all components are ready
        rb.velocity = transform.forward * _speed;

        // If data wasn't set by PayloadManager, we must have a problem.
        if (_speed <= 0)
        {
            Debug.LogError($"Rocket speed is zero! Did PayloadManager call SetPayloadData()?");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called immediately after instantiation by the PayloadManager to initialize properties.
    /// </summary>
    public void SetPayloadData(float speed, int damage, float lifeTime)
    {
        _speed = speed;
        _damage = damage;
        _lifeTime = lifeTime;
        Debug.Log($"Rocket data set: Speed={_speed}, Damage={_damage}");
    }

    // Update is called once per frame
    void Update()
    {
        // Check for null before playing particle system
        if (missileBurn != null)
        {
            missileBurn.Play();
        }

        // Keep position locked on X and Y relative to its initial launch axes
        // NOTE: Locking position here is very restrictive and prevents horizontal movement.
        Vector3 currentPosition = transform.position;
        currentPosition.x = initialPosition.x;
        currentPosition.y = initialPosition.y;
        transform.position = currentPosition;

        // Keep rotation locked (recommended for straight flight)
        transform.rotation = initialRotation;

        // Decrease lifetime
        _lifeTime -= Time.deltaTime;

        // Check for self-destruction
        if (_lifeTime <= 0)
        {
            Destroy(gameObject);
            return;
        }

        // Check if the missile is outside the camera's view
        if (Camera.main != null)
        {
            Vector3 viewportPosition = Camera.main.WorldToViewportPoint(transform.position);
            // Destroy if out of view (left, right, top, bottom, or behind camera)
            if (viewportPosition.x < 0 || viewportPosition.x > 1 || viewportPosition.y < 0 || viewportPosition.y > 1 || viewportPosition.z < 0)
            {
                Destroy(gameObject);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyProps enemy = collision.gameObject.GetComponent<EnemyProps>();
            if (enemy != null)
            {
                // Pass synchronized damage and the missile itself as the source
                enemy.TakeDamage(_damage, this.gameObject);
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning($"Rocket hit enemy {collision.gameObject.name}, but it is missing the EnemyProps script.");
            }
        }
        else if (collision.gameObject.CompareTag("Maps"))
        {
            Destroy(gameObject);
        }
    }
}
