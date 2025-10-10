using UnityEngine;

/// <summary>
/// This behavior makes the enemy move straight forward until it goes off-screen.
/// It works in conjunction with the EnemyController to get movement speed and other properties.
/// </summary>
public class ForwardMoveBehavior : MonoBehaviour
{
    private EnemyController enemyController;

    void Start()
    {
        // Get the reference to the main EnemyController script on this same GameObject.
        enemyController = GetComponent<EnemyController>();
        if (enemyController == null)
        {
            Debug.LogError("ForwardMoveBehavior requires an EnemyController component on the same GameObject.", this);
            enabled = false;
            return;
        }
    }

    void Update()
    {
        // Only start the behavior after the initial forward movement is complete.
        if (enemyController.isInitialMovementComplete)
        {
            float speed = enemyController.enemyProps.MovSpeed;
            transform.Translate(Vector3.back * speed * Time.deltaTime);
        }
    }

    private bool IsModelInView()
    {
        if (Camera.main == null || enemyController.modelRenderer == null)
        {
            return true;
        }
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        return GeometryUtility.TestPlanesAABB(planes, enemyController.modelRenderer.bounds);
    }
}
