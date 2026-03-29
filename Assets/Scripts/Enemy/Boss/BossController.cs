using System.Linq;
using UnityEngine;

public class BossPhaseController : MonoBehaviour
{
    [Header("Boss Identity")]
    public string bossName = "Goliath Class";

    [Header("Entrance Settings")]
    public float entrySpeed = 2f;
    public float initialMovementEndZ = 15f;
    public bool isInitialMovementComplete = false;

    [Header("Phase System (Weakpoints)")]
    public BossPhase[] phases;

    private int currentPhase = -1;
    public int CurrentPhase => currentPhase;

    void Start()
    {
        // Disable ALL weakpoints at start
        foreach (var phase in phases)
        {
            foreach (var wp in phase.weakpoints)
            {
                if (wp != null)
                    wp.Deactivate();
            }
        }
    }

    void Update()
    {
        // 🚀 ENTRANCE LOGIC (UNCHANGED)
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

        // 🔁 PHASE CHECK
        if (currentPhase >= 0 && currentPhase < phases.Length)
        {
            if (phases[currentPhase].IsComplete())
            {
                StartNextPhase();
            }
        }
    }

    void StartNextPhase()
    {
        currentPhase++;

        if (currentPhase >= phases.Length)
        {
            Debug.Log($"[Boss] {bossName} defeated!");

            if (GameManager.Instance != null)
                GameManager.Instance.NotifyAllWavesCompleted();

            return;
        }

        Debug.Log($"[Boss] {bossName} Phase {currentPhase} START");

        // Activate weakpoints for this phase
        foreach (var wp in phases[currentPhase].weakpoints)
        {
            if (wp != null)
                wp.Activate();
        }
    }

    public void OnWeakpointDestroyed(BossWeakpoint wp)
    {
        Debug.Log($"[Boss] Weakpoint destroyed: {wp.name}");
    }
}