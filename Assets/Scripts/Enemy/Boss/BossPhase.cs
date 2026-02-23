using System;
using UnityEngine;

[Serializable]
public class BossPart
{
    public string partName;
    public GameObject partObject;
    public EnemyData partData;

    [Tooltip("If false, the collider will be disabled until this phase is reached.")]
    public bool canBeHitOutsidePhase = false;

    [HideInInspector] public EnemyProps props;
}

[Serializable]
public class BossPhase
{
    public string phaseName;
    public BossPart[] phaseParts;

    public bool IsPhaseComplete()
    {
        foreach (var part in phaseParts)
        {
            // If any part in this phase still has health, the phase is not over
            if (part.props != null && part.props.currentHealth > 0)
                return false;
        }
        return true;
    }
}