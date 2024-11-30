using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyFollow : MonoBehaviour
{
    public GameObject target; // Reference to the target object
    public float followSpeed = 5f; // Adjust the follow speed as needed

    void Update()
    {
        // Calculate the desired position on the X-axis
        float targetX = target.transform.position.x;
        float desiredX = Mathf.Lerp(transform.position.x, targetX, followSpeed * Time.deltaTime);

        // Maintain the Y and Z positions of the follower object
        Vector3 newPosition = new Vector3(desiredX, transform.position.y, transform.position.z);

        // Set the new position of the follower object
        transform.position = newPosition;
    }
}