using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float movSpeed;
    public float rotSpeed;
    public float maxRot = 45f;
    public float lockRadius = 113f;
    public MissileController missileController;

    Camera cam;
    Collider planecollider;
    RaycastHit hit;
    Ray ray;

    // Start is called before the first frame update
    void Start()
    {
        
        cam = GameObject.Find("Main Camera").GetComponent<Camera>();
        planecollider = GameObject.Find("Plane").GetComponent<Collider>();
    }

    // Update is called once per frame
    void Update()
    {
        //transform.position = cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 5));
        ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider == planecollider)
            {
                transform.position = Vector3.MoveTowards(transform.position, hit.point, Time.deltaTime * movSpeed);

                // Calculate the rotation angle based on movement direction
                Vector3 direction = hit.point - transform.position;
                float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

                // Clamp the angle to a desired range (adjust min and max as needed)
                float clampedAngle = Mathf.Clamp(angle, maxRot, -maxRot);

                // Smoothly rotate towards the target angle
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, clampedAngle), Time.deltaTime * rotSpeed);
            }
            else
            {
                // Smoothly rotate back to default orientation when mouse is not over the plane
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, 0, 0), Time.deltaTime * rotSpeed);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            missileController.LaunchMissile();

        }


    }
}
