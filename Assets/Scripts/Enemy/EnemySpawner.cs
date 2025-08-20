using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Tooltip("An array of enemy prefabs to choose from when spawning.")]
    public GameObject[] enemyPrefabs; // Changed to an array of GameObjects
    public GameObject player; // Reference to the player object
    public PlayerController playerController;

    void Start()
    {
        // Optional: Add a check to ensure the array is not empty
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogError("Enemy Prefabs array is empty or null! Please assign enemy prefabs in the Inspector.");
            enabled = false; // Disable the spawner if no prefabs are assigned
        }
    }

    void Update()
    {
        // Check if Keypad1 is pressed to spawn the enemy at index 0
        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            SpawnEnemy(0);
        }

        // Check if Keypad2 is pressed to spawn the enemy at index 1
        else if (Input.GetKeyDown(KeyCode.Keypad2)) // Use else if to ensure only one enemy spawns per frame
        {
            SpawnEnemy(1);
        }
        // You can continue to add more specific keypad checks for higher indices if needed
        // else if (Input.GetKeyDown(KeyCode.Keypad3))
        // {
        //     SpawnEnemy(2);
        // }
    }

    /// <summary>
    /// Spawns an enemy from the enemyPrefabs array at a random X position
    /// within the camera's viewport.
    /// </summary>
    /// <param name="prefabIndex">The index of the enemy prefab to spawn from the enemyPrefabs array.</param>
    void SpawnEnemy(int prefabIndex)
    {
        // Ensure there are prefabs to spawn and the index is valid
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("No enemy prefabs assigned to the spawner! Cannot spawn enemy.");
            return;
        }

        if (prefabIndex < 0 || prefabIndex >= enemyPrefabs.Length)
        {
            Debug.LogError($"Invalid prefab index {prefabIndex} requested! Array size is {enemyPrefabs.Length}.");
            return;
        }

        GameObject selectedEnemyPrefab = enemyPrefabs[prefabIndex];

        // Get the camera's viewport boundaries in world coordinates
        // We need the Z position of the spawner to convert viewport to world point correctly
        float cameraHeightAtSpawnerZ = Camera.main.transform.position.y - transform.position.y;
        Vector3 lowerLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, transform.position.z - Camera.main.transform.position.z));
        Vector3 upperRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, transform.position.z - Camera.main.transform.position.z));

        // Calculate the random X position within the viewport width
        float randomX = Random.Range(lowerLeft.x, upperRight.x);

        // Create the spawn position using the random X, and the spawner's Y and Z
        Vector3 spawnPosition = new Vector3(randomX, transform.position.y, transform.position.z);

        // Instantiate the selected enemy prefab at the calculated spawn position and spawner's rotation
        GameObject spawnedEnemy = Instantiate(selectedEnemyPrefab, spawnPosition, transform.rotation);

        // Get the EnemyProps script from the spawned enemy
        EnemyProps enemyProps = spawnedEnemy.GetComponent<EnemyProps>();

        if (enemyProps != null && playerController != null)
        {
            // Subscribe to the OnEnemyDestroyedByPlayer event to add score
            enemyProps.OnEnemyDestroyedByPlayer += playerController.AddScore;
            Debug.Log($"Spawned {enemyProps.EnemyName} (Index {prefabIndex}) at {spawnPosition}");
        }
        else
        {
            if (enemyProps == null)
            {
                Debug.LogError($"Spawned enemy '{selectedEnemyPrefab.name}' is missing EnemyProps script! Score event will not be registered.");
            }
            if (playerController == null)
            {
                Debug.LogError("PlayerController is not assigned to EnemySpawner! Score event will not be registered.");
            }
        }
    }
}
