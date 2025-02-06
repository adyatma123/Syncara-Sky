using UnityEngine.UI; // Or using UnityEngine.UI; if you're using a UI Text
using UnityEngine;

public class GunNameDisplay : MonoBehaviour
{
    public Gun gun; // Assign your Gun script in the Inspector
    public Text gunNameText; // Assign your TextMeshPro text object

    private void Update()
    {
        if (gun != null && gunNameText != null)
        {
            // Assuming 'guns' is a public struct in your Gun script
            gunNameText.text = gun.guns.name; // Access the 'name' property of the Guns struct.
        }
        else
        {
            Debug.LogWarning("Gun script or Gun Name Text not assigned!");
        }
    }
}