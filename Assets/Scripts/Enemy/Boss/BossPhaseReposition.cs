using UnityEngine;

public class BossPhaseReposition : MonoBehaviour
{
    public int activeDuringPhase = 2;

    public Vector3 targetPosition;
    public Vector3 targetRotation;

    public float moveSpeed = 2f;
    public float rotateSpeed = 2f;

    private BossPhaseController boss;
    private bool isActive = false;

    void Start()
    {
        boss = GetComponentInParent<BossPhaseController>();
    }

    void Update()
    {
        if (boss == null) return;

        if (boss.CurrentPhase != activeDuringPhase)
        {
            isActive = false;
            return;
        }

        isActive = true;

        // Move
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * moveSpeed
        );

        // Rotate
        Quaternion targetRot = Quaternion.Euler(targetRotation);
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRot,
            Time.deltaTime * rotateSpeed
        );
    }
}