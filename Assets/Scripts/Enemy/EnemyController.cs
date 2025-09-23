using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// This is the central manager for an enemy, handling its core properties,
/// universal movement (like initial forward motion and rotation), and shared methods.
/// The specific movement behavior is handled by a separate, dedicated component.
/// </summary>
public class EnemyController : MonoBehaviour
{
    [Header("Enemy Setup")]
    [Tooltip("Reference to the EnemyProps component on this GameObject.")]
    public EnemyProps enemyProps;

    [Tooltip("Duration of the initial forward movement before the behavior takes over.")]
    public float initialMoveDuration = 2f;
    [Tooltip("Time delay before destroying the GameObject after its model is destroyed.")]
    public float destroyDelay = 3f;

    // NOTE: This can be null, the behavior scripts need to get a reference to the player.
    [Tooltip("Adjustable follow speed for the 'FollowPlayer' behavior.")]
    public float followSpeed = 1f;

    [Tooltip("Reference to the enemy's visual model Renderer component for off-screen checks.")]
    public Renderer modelRenderer;
    [Tooltip("The maximum Z-axis rotation angle when the enemy is moving horizontally.")]
    public float maxZRotation = 15f;
    [Tooltip("The speed at which the Z-rotation interpolates back to zero.")]
    public float rotationSmoothSpeed = 5f;

    [Header("Runtime Properties (Managed by Controller)")]
    public bool isInitialMovementComplete = false;
    private float initialMoveTimer = 0f;

    // References to other components on this GameObject
    private Rigidbody rb;
    private AIShoot aiShoot;
    private float lastXPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Validate essential components
        enemyProps = GetComponent<EnemyProps>();
        if (enemyProps == null)
        {
            Debug.LogError("EnemyProps script not found on " + gameObject.name + ". EnemyController requires EnemyProps to function.", this);
            enabled = false;
            return;
        }

        // Get optional components
        aiShoot = GetComponent<AIShoot>();

        Debug.Log($"Enemy {enemyProps.EnemyName} initialized with move speed: {enemyProps.MovSpeed}");
        lastXPosition = transform.position.x;

        rb.constraints = RigidbodyConstraints.FreezePositionZ;
    }

    void Update()
    {
        // Calculate X velocity for Z-rotation
        float currentX = transform.position.x;
        float xVelocity = (currentX - lastXPosition) / Time.deltaTime;
        lastXPosition = currentX;
        RotateBasedOnXVelocity(xVelocity);

        // Initial forward movement
        if (!isInitialMovementComplete)
        {
            initialMoveTimer += Time.deltaTime;
            transform.position += Vector3.back * enemyProps.MovSpeed * Time.deltaTime;

            if (initialMoveTimer >= initialMoveDuration)
            {
                isInitialMovementComplete = true;

                // Activate shooting if applicable
                if (aiShoot != null && enemyProps.IsArmedMG)
                {
                    aiShoot.Activate();
                    Debug.Log($"Enemy {enemyProps.EnemyName} activated shooting.");
                }
            }
        }
    }

    /// <summary>
    /// Rotates the enemy around its Z-axis based on its horizontal (X) velocity.
    /// </summary>
    /// <param name="xVelocity">The velocity of the enemy along the X-axis.</param>
    private void RotateBasedOnXVelocity(float xVelocity)
    {
        float targetZRotation = 0f;
        if (xVelocity > 0.01f)
        {
            targetZRotation = -maxZRotation;
        }
        else if (xVelocity < -0.01f)
        {
            targetZRotation = maxZRotation;
        }

        Quaternion currentRotation = transform.localRotation;
        float newZRotation = Mathf.LerpAngle(currentRotation.eulerAngles.z, targetZRotation, rotationSmoothSpeed * Time.deltaTime);
        transform.localRotation = Quaternion.Euler(currentRotation.eulerAngles.x, currentRotation.eulerAngles.y, newZRotation);
    }

    // This method is now public for behavior scripts to call
    public void HandleOffScreen()
    {
        if (modelRenderer != null)
        {
            Destroy(modelRenderer.gameObject);
        }
        StartCoroutine(DestroyWithDelay());
    }

    private IEnumerator DestroyWithDelay()
    {
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
}
