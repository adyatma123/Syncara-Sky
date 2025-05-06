using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Follow : MonoBehaviour
{
    // Update is called once per frame
    public void FollowPlayer(float followSpeed)
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
}
