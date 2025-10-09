using UnityEngine;

/// <summary>
/// This behavior script makes the enemy move forward while also traversing 
/// horizontally in a sinusoidal (zigzag) pattern after the initial movement is complete.
/// </summary>
public class ZigZagBehavior : MonoBehaviour
{
    private EnemyController enemyController;

    [Header("ZigZag Configuration")]
    [Tooltip("The maximum horizontal distance (in world units) the enemy will drift from its starting X position.")]
    public float maxZigZagX = 3f;

    [Tooltip("The speed (frequency) of the horizontal oscillation. Higher values mean faster side-to-side movement.")]
    public float zigzagSpeed = 1f;

    // Stores the initial X-position to use as the center line for the zigzag motion.
    private float startXPosition;

    void Start()
    {
        // Get the reference to the main EnemyController script on this same GameObject.
        enemyController = GetComponent<EnemyController>();
        if (enemyController == null)
        {
            Debug.LogError("ZigZagBehavior requires an EnemyController component on the same GameObject.", this);
            enabled = false;
            return;
        }

        // Capture the initial X position when the enemy spawns.
        startXPosition = transform.position.x;
    }

    void Update()
    {
        // Only start the behavior after the initial forward movement is complete.
        if (enemyController.isInitialMovementComplete)
        {
            float forwardSpeed = enemyController.enemyProps.MovSpeed;
            if (forwardSpeed <= 0)
            {
                return;
            }

            // 1. Calculate new Z position (Forward movement)
            transform.Translate(Vector3.back * forwardSpeed * Time.deltaTime, Space.World);

            // 2. Calculate new X position (ZigZag movement)
            // Mathf.Sin(Time.time * zigzagSpeed) oscillates between -1 and 1.
            // We multiply this by maxZigZagX to control the amplitude (width) of the swing.
            // The result is added to the startXPosition to center the motion.
            float newX = startXPosition + Mathf.Sin(Time.time * zigzagSpeed) * maxZigZagX;

            // 3. Apply the combined position
            transform.position = new Vector3(
                newX,
                transform.position.y,
                transform.position.z
            );
        }
    }
}
