using UnityEngine;

public class ModularBossController : MonoBehaviour
{
    [Header("Boss Identity")]
    public string bossName = "Goliath Class";

    [Header("Entrance Settings")]
    public float entrySpeed = 2f;
    public float initialMovementEndZ = 15f;
    public bool isInitialMovementComplete = false; // Public for behavior access

    [Header("Phase Management")]
    public BossPhase[] phases;
    public int currentPhaseIndex = -1; // Public for behavior access

    void Start()
    {
        SetupBossParts();
    }

    private void SetupBossParts()
    {
        foreach (var phase in phases)
        {
            foreach (var part in phase.phaseParts)
            {
                if (part.partObject == null) continue;

                // 1. Setup EnemyProps
                part.props = part.partObject.GetComponent<EnemyProps>();
                if (part.props == null) part.props = part.partObject.AddComponent<EnemyProps>();
                part.props.enemyDataSource = part.partData;

                // 2. Setup Collider and initial vulnerability
                Collider col = part.partObject.GetComponent<Collider>();
                if (col != null)
                {
                    // If true, collider stays on. If false, it's disabled until its phase
                    col.enabled = part.canBeHitOutsidePhase;
                }
            }
        }
    }

    void Update()
    {
        if (!isInitialMovementComplete)
        {
            transform.position += Vector3.back * entrySpeed * Time.deltaTime;
            if (transform.position.z <= initialMovementEndZ)
            {
                isInitialMovementComplete = true;
                StartNextPhase();
            }
            return;
        }

        if (currentPhaseIndex >= 0 && currentPhaseIndex < phases.Length)
        {
            if (phases[currentPhaseIndex].IsPhaseComplete())
            {
                StartNextPhase();
            }
        }
    }

    private void StartNextPhase()
    {
        currentPhaseIndex++;

        if (currentPhaseIndex < phases.Length)
        {
            // Activate all parts in the new phase
            foreach (var part in phases[currentPhaseIndex].phaseParts)
            {
                if (part.partObject == null) continue;

                // Enable Collider so it can now take damage
                Collider col = part.partObject.GetComponent<Collider>();
                if (col != null) col.enabled = true;

                // Enable the specific controller/weapons if they exist
                EnemyController ctrl = part.partObject.GetComponent<EnemyController>();
                if (ctrl != null) ctrl.enabled = true;
            }
        }
    }
}