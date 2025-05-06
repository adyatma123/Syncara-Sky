using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public float initialMoveDistance = 10f;
    public float initialMoveDuration = 2f;
    public float destroyDelay = 3f;      // Time delay before destroying the object after the model is destroyed
    public EnemySpawner spawner;
    public float followSpeed = 5f; // Adjust the follow speed as needed
    public Renderer modelRenderer;    // Reference to the model's Renderer component
    public MonoBehaviour[] enemyBehaviors;
    public GameObject bulletPrefab; // The bullet GameObject to instantiate
    public float bulletSpeed = 200f;

    private float nextFireTime;
    private bool movingRight = false;
    private bool isPatrolling = false;
    private float initialMoveTimer = 0f;
    private bool isFollowing = false;
    private bool isOffScreen = false; // Flag to track if the model has gone off-screen

    private EnemyProps enemyProperties; // Reference to the enemyProps script
    private EnemyBullet enemyBullet; // Reference to the enemyProps script

    void Start()
    {
        // Ensure the modelRenderer is assigned
        if (modelRenderer == null)
        {
            // Attempt to get the Renderer component from the current GameObject
            modelRenderer = GetComponentInChildren<Renderer>();
            if (modelRenderer == null)
            {
                Debug.LogError("Model Renderer is not assigned! Please assign a Renderer component to the EnemyBehavior script.");
                enabled = false; // Disable the script if no renderer is found
                return;
            }
        }

        enemyProperties = GetComponent<EnemyProps>();

        if (enemyProperties == null)
        {
            Debug.LogError("EnemyProps script not found on this GameObject. Enemy speed and damage data is missing!");
            enabled = false; // Disable the script if required properties are missing
            return;
        }

        // *** FIX: Initialize nextFireTime to allow firing based on fireRate from enemyProps ***
        // Calculate the delay for the first shot based on the fire rate
        // Ensure enemyProperties is not null and fireRate is greater than 0 before calculating
        if (enemyProperties != null && enemyProperties.fireRate > 0)
        {
            // Assuming fireRate is in RPM, convert to seconds per round
            nextFireTime = Time.time + (60f / enemyProperties.fireRate);
        }
        else
        {
            // If fireRate is not set, is zero, or enemyProperties is null, default to a 1-second delay for the first shot
            nextFireTime = Time.time + 1f;
        }
    }

    void Update()
    {
        float speed = enemyProperties.movSpeed;

        // Get the speed from enemyProperties
        // Ensure enemyProperties is not null before accessing its properties
        float currentSpeed = (enemyProperties != null) ? enemyProperties.movSpeed : 0f;
        // Use a default fire rate (60 RPM = 1 RPS) if props are missing or fireRate is zero
        float currentFireRate = (enemyProperties != null && enemyProperties.fireRate > 0) ? enemyProperties.fireRate : 60f;

        // Calculate viewport boundaries
        float minX = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, 0)).x;
        float maxX = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, 0)).x;

        if (!isPatrolling)
        {
            initialMoveTimer += Time.deltaTime;
            transform.position += Vector3.back * speed * Time.deltaTime;

            if (initialMoveTimer >= initialMoveDuration)
            {
                isPatrolling = true;
                // Randomly choose between patrolling and following
                isFollowing = Random.value > 0.5f;
            }
        }
        else
        {
            if (isFollowing)
            {
                FollowPlayer();
            }
            else if (movingRight)
            {
                Patrol(minX, maxX);
            }
            else
            {
                Forward();
            }
        }

        if (Time.time >= nextFireTime)
        {
            Shoot();
            // Calculate the time for the next shot
            nextFireTime = Time.time + (60f / currentFireRate);
        }
    }

    //====================================PLAYER BEHAVIOR SCRIPTS===============================================//



    void Shoot()
    {
        if (bulletPrefab != null && enemyProperties != null)
        {
            // Instantiate the bullet at the enemy's position and rotation
            // You might want to adjust the spawn position slightly forward (e.g., transform.position + transform.forward * offset)
            GameObject instantiatedBullet = Instantiate(bulletPrefab, transform.position, transform.rotation);

            // Get the EnemyBullet script component from the instantiated bullet
            // *** FIX: Get the component from the instantiated bullet GameObject ***
            EnemyBullet bulletScript = instantiatedBullet.GetComponent<EnemyBullet>();

            // If the bullet has an EnemyBullet script, set its damage and initial movement
            if (bulletScript != null)
            {
                bulletScript.damage = (int)enemyProperties.enemyDmg; // Set bullet damage from enemyProps (cast to int if enemyDmg is float)

                bulletScript.owner = this.gameObject; // Set the enemy as the bullet's owner
                // *** Set the bullet's initial direction and speed ***
                // We want the bullet to go backwards relative to the enemy's forward direction
                Vector3 shootDirection = -transform.forward; // Shoot backwards from the enemy

                // If the EnemyBullet script has a method to set direction and speed:
                bulletScript.SetDirectionAndSpeed(shootDirection, bulletSpeed);

                // Option if using Rigidbody on the bullet prefab and applying force:
                // Rigidbody bulletRigidbody = instantiatedBullet.GetComponent<Rigidbody>(); // Use instantiatedBullet
                // if (bulletRigidbody != null)
                // {
                //     // Apply force in the calculated shootDirection
                //     bulletRigidbody.AddForce(shootDirection * bulletSpeed, ForceMode.VelocityChange); // Use VelocityChange for immediate speed
                // }
                // else
                // {
                //      Debug.LogWarning("Instantiated bullet prefab does not have a Rigidbody or a SetDirectionAndSpeed method on EnemyBullet.");
                // }

            }
            else
            {
                // This warning is correct if EnemyBullet.cs is not on the bullet prefab
                Debug.LogWarning("Instantiated bullet prefab does not have an 'EnemyBullet' script attached.");
            }
        }
        else
        {
            // This warning should ideally not happen if checks in Update are correct,
            // but good for safety.
            Debug.LogWarning("Cannot shoot: bulletPrefab or enemyProperties is null.");
        }
    }

    //Following Player Behavior
    void FollowPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            // Calculate the desired position on the X-axis
            float targetX = player.transform.position.x;
            float desiredX = Mathf.Lerp(transform.position.x, targetX, followSpeed * Time.deltaTime);

            // Maintain the Y and Z positions of the follower object
            Vector3 newPosition = new Vector3(desiredX, transform.position.y, transform.position.z);

            // Set the new position of the follower object
            transform.position = newPosition;
        }
    }

    void Patrol(float minX, float maxX)
    {
        float speed = enemyProperties.movSpeed;

        if (movingRight)
        {
            transform.position += Vector3.right * speed * Time.deltaTime;

            if (transform.position.x >= maxX)
            {
                movingRight = false;
            }
        }
        else
        {
            transform.position -= Vector3.right * speed * Time.deltaTime;

            if (transform.position.x <= minX)
            {
                movingRight = true;
            }
        }
    }
    void Forward()
    {
        float speed = enemyProperties.movSpeed;

        // Move the enemy forward
        transform.Translate(Vector3.back * speed * Time.deltaTime);

        // Check if the model is off the camera's view
        if (!IsModelInView() && !isOffScreen)
        {
            isOffScreen = true;
            HandleOffScreen();
        }
    }

    //===================================================================================//

    bool IsModelInView()
    {
        if (Camera.main == null)
        {
            Debug.LogError("Main Camera is null.  Make sure you have a Camera tagged as MainCamera in your scene.");
            return true; // Return true to prevent premature destruction.  You could also disable the script.
        }
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        return GeometryUtility.TestPlanesAABB(planes, modelRenderer.bounds);
    }

    void HandleOffScreen()
    {
        // 1. Destroy the model (Renderer)
        if (modelRenderer != null)
        {
            Destroy(modelRenderer.gameObject);
        }

        // 2. Start the coroutine to destroy the entire object after a delay
        StartCoroutine(DestroyWithDelay());
    }

    IEnumerator DestroyWithDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject); // Destroy the entire GameObject this script is attached to
    }
}