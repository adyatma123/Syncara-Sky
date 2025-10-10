using UnityEngine.UI; // Or using UnityEngine.UI; if you're using a UI Text
using UnityEngine;

public class GunNameDisplay : MonoBehaviour
{
    public Gun gun; // Assign your Gun script in the Inspector
    public Text gunNameText; // Assign your TextMeshPro text object
    private bool gunFound = false; // Added a flag to track if the gun is found

    private void Update()
    {
        if (!gunFound) // Only search for the gun if it hasn't been found yet
        {
            // Find the Gun script.
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                gun = player.GetComponent<Gun>();
                if (gun != null)
                {
                    gunFound = true; // Stop searching once found
                }
                else
                {
                    Debug.LogError("Gun script not found on Player!");
                }
            }
            else
            {
                //Debug.LogError("No object with 'Player' tag found!"); // Removed this error message because the player might not exist at the very beginning of the scene.  This prevents a spam of errors.
            }
        }

        if (gun != null && gunNameText != null)
        {
            // Assuming 'guns' is a public struct in your Gun script
            gunNameText.text = gun.guns.name; // Access the 'name' property of the Guns struct.
        }
    }
}