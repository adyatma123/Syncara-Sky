using UnityEngine;

/// <summary>
/// This behavior makes the enemy follow the player's horizontal position after the initial move.
/// It works in conjunction with the EnemyController to get movement speed and other properties.
/// </summary>
public class FollowPlayerBehavior : MonoBehaviour, IEnemyBehavior // <-- IMPLEMENT IEnemyBehavior
{
    private EnemyController enemyController;
    private GameObject player;

    void Start()
    {
        // Get the reference to the main EnemyController script on this same GameObject.
        enemyController = GetComponent<EnemyController>();
        if (enemyController == null)
        {
            Debug.LogError("FollowPlayerBehavior requires an EnemyController component on the same GameObject.", this);
            enabled = false;
            return;
        }

        // Find the player once during initialization for performance
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("Player not found! The FollowPlayer behavior will not function correctly.");
        }
    }

    void Update()
    {
        // Only start the behavior after the initial forward movement is complete.
        if (enemyController.isInitialMovementComplete)
        {
            if (player != null)
            {
                float targetX = player.transform.position.x;
                float desiredX = Mathf.Lerp(
                    transform.position.x,
                    targetX,
                    enemyController.followSpeed * Time.deltaTime
                );

                Vector3 newPosition = new Vector3(desiredX, transform.position.y, transform.position.z);
                transform.position = newPosition;
            }
        }
    }
}
