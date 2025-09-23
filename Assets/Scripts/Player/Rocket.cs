using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rocket : MonoBehaviour
{
    public float speed;
    public int damage;
    public int lifeTime;

    Rigidbody rb;
    public ParticleSystem missileBurn;
    private Vector3 initialPosition; // Store initial position
    private Quaternion initialRotation; // Store initial rotation

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        initialPosition = transform.position; // Record initial position
        initialRotation = transform.rotation; // Record initial rotation
    }

    // Update is called once per frame
    void Update()
    {
        missileBurn.Play();

        // Keep position locked on X and Y
        Vector3 currentPosition = transform.position;
        currentPosition.x = initialPosition.x;
        currentPosition.y = initialPosition.y;
        transform.position = currentPosition;

        // Keep rotation locked (optional, but recommended for straight flight)
        transform.rotation = initialRotation;

        rb.velocity = transform.forward * speed * Time.fixedDeltaTime * 50f;

        // Check if the bullet is outside the camera's view
        Vector3 viewportPosition = Camera.main.WorldToViewportPoint(transform.position);
        if (viewportPosition.x < 0 || viewportPosition.x > 1 || viewportPosition.y < 0 || viewportPosition.y > 1 || viewportPosition.z < 0)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            EnemyProps enemy = collision.gameObject.GetComponent<EnemyProps>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
                Destroy(gameObject);
            }
        }
        else if (collision.gameObject.CompareTag("Maps"))
        {
            Destroy(gameObject);
        }
        // If it's not "Enemy" or "Maps", do nothing (ignore the collision)
    }
}
