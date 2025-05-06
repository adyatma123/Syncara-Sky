using UnityEngine;
using UnityEngine.UI; // Required for Image

public class HeatBar : MonoBehaviour
{
    public RectTransform heatBarRect;
    public Image heatImage; // Assign your Image in the Inspector
    public Gun gun; // Assign the script that has the heat variable

    private bool gunFound = false; // Added a flag to track if the gun is found
    private float initialHeight;

    private void Start()
    {
        if (heatBarRect != null)
        {
            initialHeight = heatBarRect.rect.height;
        }
        else
        {
            Debug.LogError("Heat bar Rect Transform not assigned!");
        }
    }

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

        if (heatBarRect != null && gun != null)
        {
            float heat = gun.currentHeat;
            float maxHeat = gun.maxHeat;

            float newHeight = initialHeight * (1f - (heat / maxHeat));
            newHeight = Mathf.Clamp(newHeight, 0f, initialHeight);

            heatBarRect.sizeDelta = new Vector2(heatBarRect.sizeDelta.x, newHeight);

            // *** COLOR CHANGE (Green to Halfway, then to Red) ***
            float lerpFactor;
            if (heat <= maxHeat / 2f) // First half: Green to halfway
            {
                lerpFactor = heat / (maxHeat / 2f); // 0 to 1
                heatColor = Color.Lerp(Color.green, new Color(1f, 0.5f, 0f), lerpFactor); // Green to orange/yellow
            }
            else // Second half: Halfway to Red
            {
                lerpFactor = (heat - (maxHeat / 2f)) / (maxHeat / 2f); // 0 to 1
                heatColor = Color.Lerp(new Color(1f, 0.5f, 0f), Color.red, lerpFactor); // Orange/yellow to red
            }

            heatImage.color = heatColor;
        }
    }

    private Color heatColor; // Store the intermediate color to avoid recalculation
}