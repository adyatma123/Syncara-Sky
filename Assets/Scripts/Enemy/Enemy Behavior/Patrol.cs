using UnityEngine;

/// <summary>
/// This behavior script makes the enemy patrol back and forth horizontally within the screen bounds.
/// It works in conjunction with the EnemyController to get movement speed and other properties.
/// </summary>
public class PatrolBehavior : MonoBehaviour
{
    private EnemyController enemyController;
    private bool movingRight = false;

    void Start()
    {
        // Get the reference to the main EnemyController script on this same GameObject.
        enemyController = GetComponent<EnemyController>();
        if (enemyController == null)
        {
            Debug.LogError("PatrolBehavior requires an EnemyController component on the same GameObject.", this);
            enabled = false;
            return;
        }

        // Randomly choose the initial patrol direction
        movingRight = Random.value > 0.5f;
    }

    void Update()
    {
        // Only start patrolling after the initial forward movement is complete.
        if (enemyController.isInitialMovementComplete)
        {
            float speed = enemyController.enemyProps.MovSpeed;
            if (speed <= 0)
            {
                return;
            }

            // Calculate viewport boundaries dynamically
            float minX = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, transform.position.z - Camera.main.transform.position.z)).x;
            float maxX = Camera.main.ViewportToWorldPoint(new Vector3(1, 0, transform.position.z - Camera.main.transform.position.z)).x;

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
    }
}
