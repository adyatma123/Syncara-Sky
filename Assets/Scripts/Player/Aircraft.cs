using UnityEngine;

public class AircraftController : MonoBehaviour
{
    [Header("Aircraft Properties")]
    public int health = 1000;

    [Header("Movement Settings")]
    public float movSpeed = 10f;
    public float rotSpeed = 5f;
    public float maxRotAngle = 45f;

    [Header("Weapon Systems")]
    public MissileController missileController;
    public RocketController rocketController;

    private Vector3 targetPosition;
    private bool hasTargetPosition = false;

    // Update is called once per frame
    void Update()
    {
        if (hasTargetPosition)
        {
            MoveTowardsTarget();
            RotateTowardsMovement();
        }
        else
        {
            RotateBackToDefault();
        }
    }

    public void SetTargetPosition(Vector3 target)
    {
        targetPosition = target;
        hasTargetPosition = true;
    }

    void MoveTowardsTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, Time.deltaTime * movSpeed);
    }

    void RotateTowardsMovement()
    {
        Vector3 directionToTarget = targetPosition - transform.position;
        Vector3 projectedDirection = Vector3.ProjectOnPlane(directionToTarget, Vector3.up).normalized;

        if (projectedDirection != Vector3.zero)
        {
            float targetAngleZ = -Mathf.Atan2(projectedDirection.x, projectedDirection.z) * Mathf.Rad2Deg;
            float clampedAngleZ = Mathf.Clamp(targetAngleZ, -maxRotAngle, maxRotAngle);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, clampedAngleZ), Time.deltaTime * rotSpeed);
        }
    }

    public void TakeDamage(int damageAmount)
    {
        health -= damageAmount;

        if (health <= 0)
        {
            Destroy(gameObject);
            AudioManager.Instance.PlaySFX("Explode");
        }
    }

    public void ResetRotation()
    {
        hasTargetPosition = false;
        RotateBackToDefault();
    }

    void RotateBackToDefault()
    {
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, Time.deltaTime * rotSpeed);
    }

    public void FireMissile()
    {
        if (missileController != null)
        {
            missileController.LaunchMissile();
        }
        else
        {
            Debug.LogWarning("MissileController not assigned to AircraftController!");
        }
    }

    public void FireRocket()
    {
        if (rocketController != null)
        {
            rocketController.LaunchRocket();
        }
        else
        {
            Debug.LogWarning("RocketController not assigned to AircraftController!");
        }
    }
}