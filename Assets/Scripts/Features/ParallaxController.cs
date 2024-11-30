using UnityEngine;
using UnityEngine.Tilemaps;


   public class ParallaxController : MonoBehaviour
    {
    public SpriteRenderer spriteRenderer;
    public float parallaxFactor = 0.5f;
    public float spriteWidth;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        float parallaxX = Camera.main.transform.position.x * parallaxFactor;
        transform.position = new Vector3(startPosition.x + parallaxX, transform.position.y, transform.position.z);

        // Check for looping
        if (transform.position.x < -spriteWidth)
        {
            transform.position += new Vector3(spriteWidth * 2, 0, 0);
        }
        else if (transform.position.x > spriteWidth)
        {
            transform.position -= new Vector3(spriteWidth * 2, 0, 0);
        }
    }
    }