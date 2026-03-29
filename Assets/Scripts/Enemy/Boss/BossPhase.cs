using UnityEngine;

[System.Serializable]
public class BossPhase
{
    public BossWeakpoint[] weakpoints;

    public bool IsComplete()
    {
        foreach (var wp in weakpoints)
        {
            if (wp != null && wp.gameObject.activeSelf)
                return false;
        }
        return true;
    }
}