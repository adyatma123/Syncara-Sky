using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    public float minX;
    public float maxX;
    public float speed = 100f;

    private bool movingRight = true;

    void Update()
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