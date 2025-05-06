using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab; // Reference to the enemy prefab
    public GameObject player; // Reference to the player object
    public PlayerController playerController;

    void Start()
    {
        // Spawn the enemy at the start of the game
        SpawnEnemy();
    }

    void Update()
    {
        if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
        {
            SpawnEnemy();
        }
    }

    void SpawnEnemy()
    {
        // Get the camera's viewport boundaries in world coordinates
        // We need the Z position of the spawner to convert viewport to world point correctly
        float cameraHeightAtSpawnerZ = Camera.main.transform.position.y - transform.position.y; // Assuming spawner is below camera
        Vector3 lowerLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, transform.position.z - Camera.main.transform.position.z));
        Vector3 upperRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, transform.position.z - Camera.main.transform.position.z));


        // Calculate the random X position within the viewport width
        float randomX = Random.Range(lowerLeft.x, upperRight.x);

        // Create the spawn position using the random X, and the spawner's Y and Z
        Vector3 spawnPosition = new Vector3(randomX, transform.position.y, transform.position.z);

        // Instantiate the enemy at the calculated spawn position and spawner's rotation
        GameObject spawnedEnemy = Instantiate(enemyPrefab, spawnPosition, transform.rotation);

        // *** Get the EnemyProps script from the spawned enemy ***
        EnemyProps enemyProps = spawnedEnemy.GetComponent<EnemyProps>();

        enemyProps.OnEnemyDestroyedByPlayer += playerController.AddScore;

        Instantiate(enemyPrefab, spawnPosition, transform.rotation);
    }
}
