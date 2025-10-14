using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorManager : MonoBehaviour
{
    // Enumeration to easily switch between common hotspot types
    public enum HotspotType { Arrow, Crosshair }

    [Header("Cursor Settings")]
    [Tooltip("The base texture used for the cursor.")]
    [SerializeField] private Texture2D baseCursorTexture;

    [Tooltip("The desired size for the cursor (X and Y are linked).")]
    [Range(16, 64)]
    public int cursorSize = 32;

    [Tooltip("Selects the hotspot (active click point) for the cursor.")]
    public HotspotType hotspotMode = HotspotType.Crosshair;

    private Texture2D _sizedCursorTexture;
    private Vector2 _cursorHotspot;
    private bool _isInitialized = false; // Track if Start has run

    // Start is called before the first frame update
    void Start()
    {
        // Check if the texture exists
        if (baseCursorTexture == null)
        {
            Debug.LogError("Base Cursor Texture is not assigned in the Inspector!");
            return;
        }

        SetCustomCursor();
        _isInitialized = true;
    }

    /// <summary>
    /// PUBLIC API: Generates a new Texture2D at the desired size, 
    /// calculates the hotspot, and sets the cursor.
    /// </summary>
    public void SetCustomCursor()
    {
        // 1. Resize/Generate the Texture using the much faster and reliable Graphics.Blit
        _sizedCursorTexture = ResizeTextureGPU(baseCursorTexture, cursorSize, cursorSize);

        // 2. Calculate Hotspot based on the selected mode
        CalculateHotspot();

        // 3. Set the Cursor
        // Unity requires the texture used by SetCursor to be readable, even if 
        // Graphics.Blit was used. It's best practice to destroy the old one.
        Cursor.SetCursor(_sizedCursorTexture, _cursorHotspot, CursorMode.ForceSoftware);
    }

    /// <summary>
    /// Calculates and sets the Vector2 hotspot based on the selected HotspotType.
    /// </summary>
    private void CalculateHotspot()
    {
        switch (hotspotMode)
        {
            case HotspotType.Arrow:
                // Hotspot near the top-left corner (0,0) is typical for an arrow's tip.
                // We assume the tip is at (0, 0) of the new texture.
                _cursorHotspot = new Vector2(0, 0);
                break;

            case HotspotType.Crosshair:
            default:
                // Hotspot exactly in the center is required for crosshair/circular cursors.
                // Divide the size by 2 to get the center coordinate.
                _cursorHotspot = new Vector2(cursorSize / 2f, cursorSize / 2f);
                break;
        }
    }

    /// <summary>
    /// Utility function to resize a texture using the GPU (Graphics.Blit).
    /// This is the recommended method for fast, reliable texture resizing.
    /// The source texture does NOT need Read/Write enabled for this method.
    /// </summary>
    private Texture2D ResizeTextureGPU(Texture2D source, int newWidth, int newHeight)
    {
        // Check for existing texture and destroy it to avoid memory leak
        if (_sizedCursorTexture != null)
        {
            DestroyImmediate(_sizedCursorTexture);
        }

        // 1. Create a temporary RenderTexture
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);

        // 2. Blit (copy and scale) the source texture onto the render texture
        Graphics.Blit(source, rt);

        // 3. Create the final Texture2D
        Texture2D newTexture = new Texture2D(newWidth, newHeight);

        // 4. Copy the pixels from the render texture to the new Texture2D
        RenderTexture.active = rt;
        newTexture.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        newTexture.Apply();

        // 5. Clean up the temporary resources
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return newTexture;
    }

    // This method runs in the Editor whenever a public field is changed, allowing for instant preview.
    private void OnValidate()
    {
        // If the game is running OR if the texture is not null, try to update the cursor.
        // This ensures the size/hotspot updates visually in the Inspector.
        if (Application.isPlaying || _isInitialized)
        {
            if (baseCursorTexture != null)
            {
                SetCustomCursor();
            }
        }
    }

    // Clean up the generated texture when the object is destroyed
    private void OnDestroy()
    {
        if (_sizedCursorTexture != null)
        {
            Destroy(_sizedCursorTexture);
        }
    }
}
