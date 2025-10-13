// GameSelectionManager.cs (Revised)
using UnityEngine;

/// <summary>
/// A persistent Singleton that holds selected game state data across scenes.
/// </summary>
public class GameSelectionManager : MonoBehaviour
{
    public static GameSelectionManager Instance { get; private set; }

    // This is the data we need to persist. It starts as null.
    public Guns ConfirmedGunSelection { get; private set; }

    private void Awake()
    {
        // Singleton setup: Ensure only one instance exists and persists.
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// PUBLIC: Called by GunSelector when the player confirms their choice.
    /// </summary>
    /// <param name="gunData">The ScriptableObject of the selected gun.</param>
    public void SetConfirmedGun(Guns gunData)
    {
        ConfirmedGunSelection = gunData;
        Debug.Log($"Selection Manager: Confirmed gun set to {gunData.name}.");
    }
}