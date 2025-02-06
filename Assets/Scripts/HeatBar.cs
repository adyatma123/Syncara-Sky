using UnityEngine;
using UnityEngine.UI; // Required for Image

public class HeatBar : MonoBehaviour
{
    public RectTransform heatBarRect;
    public Image heatImage; // Assign your Image in the Inspector
    public Gun gun; // Assign the script that has the heat variable

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