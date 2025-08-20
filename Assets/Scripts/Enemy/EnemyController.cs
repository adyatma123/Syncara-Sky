using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; // Required for Action event if you want EnemyController to listen to OnEnemyDestroyedByPlayer

public class EnemyController : MonoBehaviour
{
    [Header("Enemy Setup")]
    [Tooltip("Reference to the EnemyProps component on this GameObject.")]
    public EnemyProps enemyProps; // Reference to the EnemyProps MonoBehaviour

    [Tooltip("Duration of the initial forward movement before choosing a main behavior.")]
    public float initialMoveDuration = 2f;
    [Tooltip("Time delay before destroying the GameObject after the model is destroyed (e.g., after an explosion effect).")]
    public float destroyDelay = 3f;
    public EnemySpawner spawner; // Consider if this is still needed directly or can be handled via events

    [Tooltip("Adjustable follow speed for the 'FollowPlayer' behavior.")]
    public float followSpeed = 5f;
    [Tooltip("Reference to the enemy's visual model Renderer component for off-screen checks.")]
    public Renderer modelRenderer;
    public GameObject bulletPrefab; // The bullet GameObject to instantiate
    [Tooltip("The initial speed at which bullets fired by this enemy travel.")]
    public float bulletSpeed = 200f;
    [Tooltip("The maximum Z-axis rotation angle when the enemy is moving horizontally.")]
    public float maxZRotation = 15f; // New public variable for max Z rotation
    [Tooltip("The speed at which the Z-rotation interpolates back to zero.")]
    public float rotationSmoothSpeed = 5f; // New public variable for rotation smoothing

    [Header("Runtime Properties (Managed by Controller)")]
    private float nextFireTime;
    private bool movingRight = false; // Used specifically for the Patrol behavior
    private bool isInitialMovementComplete = false;
    private float initialMoveTimer = 0f;
    private bool isOffScreen = false;

    // New: To track the previous X position for velocity calculation
    private float lastXPosition;

    // Define an enum to clearly differentiate between the enemy's potential behaviors
    private enum EnemyMovementBehavior
    {
        FollowPlayer,
        Patrol,
        ForwardMove
    }

    private EnemyMovementBehavior chosenBehavior; // The specific behavior chosen for this enemy

    void Start()
    {
        // Validate essential components and data
        enemyProps = GetComponent<EnemyProps>();
        if (enemyProps == null)
        {
            Debug.LogError("EnemyProps script not found on " + gameObject.name + ". EnemyController requires EnemyProps to function.", this);
            enabled = false; // Disable the script if EnemyProps is missing
            return;
        }

        if (modelRenderer == null)
        {
            modelRenderer = GetComponentInChildren<Renderer>();
            if (modelRenderer == null)
            {
                Debug.LogError("Model Renderer is not assigned! Please assign a Renderer component to the EnemyController script on " + gameObject.name + ".", this);
                enabled = false;
                return;
            }
        }

        // Log the enemy's name and movement speed from the EnemyProps component
        Debug.Log($"Enemy {enemyProps.EnemyName} initialized with move speed: {enemyProps.MovSpeed}");
        Debug.Log($"Enemy {enemyProps.EnemyName} is Helicopter: {enemyProps.IsHelicopter}, Armed MG: {enemyProps.IsArmedMG}, Armed RKT: {enemyProps.IsArmedRKT}, Armed MSL: {enemyProps.IsArmedMSL}");


        // Initialize nextFireTime based on FireRate from EnemyProps
        if (enemyProps.FireRate > 0)
        {
            nextFireTime = Time.time + (60f / enemyProps.FireRate);
        }
        else
        {
            nextFireTime = Time.time + 1f; // Default to a 1-second delay if fireRate is invalid
        }

        // Initialize lastXPosition with the current X position
        lastXPosition = transform.position.x;
    }

    void Update()
    {
        // Calculate X velocity
        float currentX = transform.position.x;
        float xVelocity = (currentX - lastXPosition) / Time.deltaTime;
        lastXPosition = currentX; // Update lastXPosition for the next frame

        // Apply Z-rotation based on X velocity
        RotateBasedOnXVelocity(xVelocity);

        float speed = enemyProps.MovSpeed;
        float currentFireRate = enemyProps.FireRate > 0 ? enemyProps.FireRate : 60f;

        float minX = 0f;
        float maxX = 0f;

        // Calculate viewport boundaries for Patrol behavior
        if (Camera.main == null)
        {
            Debug.LogError("Main Camera is null! Cannot calculate viewport boundaries for enemy movement. Please tag a Camera as 'MainCamera' in your scene.");
            return;
        }
        else
        {
            minX = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, transform.position.z - Camera.main.transform.position.z)).x;
            maxX = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, transform.position.z - Camera.main.transform.position.z)).x;
        }

        if (!isInitialMovementComplete)
        {
            initialMoveTimer += Time.deltaTime;
            transform.position += Vector3.back * speed * Time.deltaTime;

            if (initialMoveTimer >= initialMoveDuration)
            {
                isInitialMovementComplete = true;

                if (enemyProps.IsHelicopter)
                {
                    int randomBehaviorIndex = UnityEngine.Random.Range(0, 2);
                    switch (randomBehaviorIndex)
                    {
                        case 0:
                            chosenBehavior = EnemyMovementBehavior.FollowPlayer;
                            Debug.Log($"Enemy {enemyProps.EnemyName} (Helicopter) chosen behavior: FollowPlayer");
                            break;
                        case 1:
                            chosenBehavior = EnemyMovementBehavior.Patrol;
                            movingRight = UnityEngine.Random.value > 0.5f;
                            Debug.Log($"Enemy {enemyProps.EnemyName} (Helicopter) chosen behavior: Patrol (starts movingRight: {movingRight}) at X: {transform.position.x}, minX: {minX}, maxX: {maxX}");
                            break;
                    }
                }
                else
                {
                    chosenBehavior = EnemyMovementBehavior.ForwardMove;
                    Debug.Log($"Enemy {enemyProps.EnemyName} (Non-Helicopter) chosen behavior: ForwardMove");
                }
            }
        }
        else // Once the initial movement is complete, execute the chosen randomized behavior
        {
            switch (chosenBehavior)
            {
                case EnemyMovementBehavior.FollowPlayer:
                    FollowPlayer();
                    break;
                case EnemyMovementBehavior.Patrol:
                    Patrol(minX, maxX);
                    break;
                case EnemyMovementBehavior.ForwardMove:
                    Forward();
                    break;
            }
        }

        // Handle shooting regardless of movement behavior
        if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + (60f / currentFireRate);
        }
    }

    /// <summary>
    /// Rotates the enemy around its Z-axis based on its horizontal (X) velocity.
    /// Moves left -> rotates Z positive (leans right)
    /// Moves right -> rotates Z negative (leans left)
    /// No movement -> rotates Z back to 0
    /// </summary>
    /// <param name="xVelocity">The velocity of the enemy along the X-axis.</param>
    void RotateBasedOnXVelocity(float xVelocity)
    {
        float targetZRotation = 0f;

        // Determine target Z rotation based on X velocity
        if (xVelocity > 0.01f) // Moving right
        {
            targetZRotation = -maxZRotation; // Negative Z rotation for leaning left
        }
        else if (xVelocity < -0.01f) // Moving left
        {
            targetZRotation = maxZRotation; // Positive Z rotation for leaning right
        }
        // If velocity is near zero, targetZRotation remains 0

        // Smoothly interpolate the current Z rotation towards the target Z rotation
        Quaternion currentRotation = transform.localRotation;
        float newZRotation = Mathf.LerpAngle(currentRotation.eulerAngles.z, targetZRotation, rotationSmoothSpeed * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(currentRotation.eulerAngles.x, currentRotation.eulerAngles.y, newZRotation);
    }

    void Shoot()
    {
        if (bulletPrefab != null && enemyProps != null)
        {
            GameObject instantiatedBullet = Instantiate(bulletPrefab, transform.position, transform.rotation);
            EnemyBullet bulletScript = instantiatedBullet.GetComponent<EnemyBullet>();

            if (bulletScript != null)
            {
                bulletScript.damage = (int)enemyProps.EnemyDmg;
                bulletScript.owner = this.gameObject;
                Vector3 shootDirection = -transform.forward;
                bulletScript.SetDirectionAndSpeed(shootDirection, bulletSpeed);
            }
            else
            {
                Debug.LogWarning("Instantiated bullet prefab does not have an 'EnemyBullet' script attached.");
            }
        }
        else
        {
            Debug.LogWarning("Cannot shoot: bulletPrefab or enemyProps is null.");
        }
    }

    void FollowPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            float targetX = player.transform.position.x;
            float desiredX = Mathf.Lerp(transform.position.x, targetX, followSpeed * Time.deltaTime);

            Vector3 newPosition = new Vector3(desiredX, transform.position.y, transform.position.z);
            transform.position = newPosition;
        }
    }

    void Patrol(float minX, float maxX)
    {
        float speed = enemyProps.MovSpeed;

        if (speed <= 0)
        {
            Debug.LogWarning($"Patrol speed is zero or negative ({speed}) for {enemyProps.EnemyName}. Enemy will not move. Check assigned EnemyData asset.");
            return;
        }

        if (movingRight)
        {
            transform.position += Vector3.right * speed * Time.deltaTime;

            if (transform.position.x >= maxX)
            {
                movingRight = false;
                Debug.Log($"Patrol: {enemyProps.EnemyName} hit MaxX ({maxX}), reversing to left. Current X: {transform.position.x}");
            }
        }
        else
        {
            transform.position -= Vector3.right * speed * Time.deltaTime;

            if (transform.position.x <= minX)
            {
                movingRight = true;
                Debug.Log($"Patrol: {enemyProps.EnemyName} hit MinX ({minX}), reversing to right. Current X: {transform.position.x}");
            }
        }
    }

    void Forward()
    {
        float speed = enemyProps.MovSpeed;

        transform.Translate(Vector3.back * speed * Time.deltaTime);

        if (!IsModelInView() && !isOffScreen)
        {
            isOffScreen = true;
            HandleOffScreen();
        }
    }

    bool IsModelInView()
    {
        if (Camera.main == null)
        {
            Debug.LogError("Main Camera is null. Make sure you have a Camera tagged as MainCamera in your scene.");
            return true;
        }
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        return GeometryUtility.TestPlanesAABB(planes, modelRenderer.bounds);
    }

    void HandleOffScreen()
    {
        if (modelRenderer != null)
        {
            Destroy(modelRenderer.gameObject);
        }
        StartCoroutine(DestroyWithDelay());
    }

    IEnumerator DestroyWithDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}
