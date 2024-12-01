using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public float minX;
    public float maxX;
    public float speed = 100f;
    public float initialMoveDistance = 10f;
    public float initialMoveDuration = 2f;
    public EnemySpawner spawner;
    public float followSpeed = 5f; // Adjust the follow speed as needed

    private bool movingRight = false;
    private bool isPatrolling = false;
    private float initialMoveTimer = 0f;
    private bool isFollowing = false;


    void Update()
    {
        
        if (!isPatrolling)
        {
            initialMoveTimer += Time.deltaTime;
            transform.position += Vector3.back * speed * Time.deltaTime;

            if (initialMoveTimer >= initialMoveDuration)
            {
                isPatrolling = true;
                // Randomly choose between patrolling and following
                isFollowing = Random.value > 0.5f;
            }
        }
        else
        {
            if (isFollowing)
            {
                FollowPlayer();
            }
            else
            {
                Patrol();
            }
        }
    }

    //Following Player Behavior
    void FollowPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            // Calculate the desired position on the X-axis
            float targetX = player.transform.position.x;
            float desiredX = Mathf.Lerp(transform.position.x, targetX, followSpeed * Time.deltaTime);

            // Maintain the Y and Z positions of the follower object
            Vector3 newPosition = new Vector3(desiredX, transform.position.y, transform.position.z);

            // Set the new position of the follower object
            transform.position = newPosition;
        }
    }

    void Patrol()
    {
        if (movingRight)
        {
            transform.position += Vector3.right * speed * Time.deltaTime;

            if (transform.position.x >= maxX)
            {
                movingRight = false;
            }
        }
        else
        {
            transform.position -= Vector3.right * speed * Time.deltaTime;

            if (transform.position.x <= minX)
            {
                movingRight = true;
            }
        }
    }
}