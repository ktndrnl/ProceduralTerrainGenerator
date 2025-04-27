// TextureData.cs
//
// Description:
// Scriptable Object that manages terrain texture generation and color banding
// for procedural terrain. Handles the creation and management of height-based
// color gradients, allowing for realistic terrain coloring based on elevation
// (e.g., water at low elevations, snow at peaks).
//
// Key Features:
// - Height-based color banding
// - Dynamic texture generation
// - Shader property management
// - Material integration
// - Customizable color thresholds


using UnityEngine;

/// <summary>
/// Manages terrain texturing and color banding based on height values.
/// Create via: Create > Scriptable Objects > TextureData
/// </summary>
[CreateAssetMenu(fileName = "TextureData", menuName = "Scriptable Objects/TextureData")]
public class TextureData : UpdatableData
{
    /// <summary>
    /// Cached shader property IDs for efficient material updates
    /// </summary>
    private static readonly int MinHeight = Shader.PropertyToID("_MinHeight");
    private static readonly int MaxHeight = Shader.PropertyToID("_MaxHeight");
    private static readonly int Colors = Shader.PropertyToID("_Colors");

    [Header("Color Bands")]
    /// <summary>
    /// Array of colors for different height bands
    /// </summary>
    public Color[] baseColors;

    /// <summary>
    /// Normalized height values where each color band starts
    /// Must be same length as baseColors and in ascending order [0-1]
    /// </summary>
    [Range(0, 1)]
    public float[] baseStartHeights;
    // e.g. baseColors = [blue, green, brown, white]
    //      baseStartHeights = [0.0, 0.4, 0.7, 1.0]

    [Header("Generated Texture")]
    /// <summary>
    /// Generated or assigned texture containing color band information
    /// </summary>
    public Texture2D colors;

    private Texture2D defaultTexture = null;

    private float savedMinHeight;
    private float savedMaxHeight;

    /// <summary>
    /// Applies texture settings to a material, generating texture if needed
    /// </summary>
    /// <param name="material">Target material for texture application</param>
    public void ApplyToMaterial(Material material)
    {
        if (colors == null)
        {
            if (defaultTexture == null)
            {
                defaultTexture = GenerateColorBandTexture(256);
            }
            colors = defaultTexture;
        }

        material.SetTexture("_Colors", colors);
        UpdateMeshHeights(material, savedMinHeight, savedMaxHeight);
    }

    /// <summary>
    /// Generates a 1D texture containing discrete color bands
    /// </summary>
    /// <param name="width">Width of the texture in pixels</param>
    /// <returns>Generated color band texture</returns>
    private Texture2D GenerateColorBandTexture(int width)
    {
        // Create a 1D texture; height = 1
        var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        // Prepare an array to store the pixel colors
        var pixelColors = new Color[width];

        for (int x = 0; x < width; x++)
        {
            // Normalized "height" from [0..1]
            float t = x / (float)(width - 1);

            // Figure out which band t belongs to, based on baseStartHeights
            // For example, if baseStartHeights = [0.0, 0.4, 0.7, 1.0],
            // then anything from 0.0..0.4 is band 0, 0.4..0.7 is band 1, etc.
            Color bandColor = GetColorForHeight(t, baseStartHeights, baseColors);
            pixelColors[x] = bandColor;
        }

        tex.SetPixels(pixelColors);
        tex.Apply();

        return tex;
    }

    /// <summary>
    /// Determines the appropriate color for a given normalized height value
    /// </summary>
    /// <param name="t">Normalized height value [0-1]</param>
    /// <param name="thresholds">Array of height thresholds</param>
    /// <param name="colors">Array of colors corresponding to thresholds</param>
    /// <returns>Color for the given height</returns>
    private static Color GetColorForHeight(float t, float[] thresholds, Color[] colors)
    {
        // Assumes thresholds.Length == colors.Length
        for (int i = 0; i < thresholds.Length; i++)
        {
            // If our t is less than or equal the threshold, use that color
            if (t <= thresholds[i])
            {
                return colors[i];
            }
        }
        // Fallback: if t is beyond last threshold, return last color
        return colors[^1];
    }

    /// <summary>
    /// Updates material with new height range and texture settings
    /// </summary>
    /// <param name="material">Target material to update</param>
    /// <param name="minHeight">Minimum terrain height</param>
    /// <param name="maxHeight">Maximum terrain height</param>
    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
    {
        savedMinHeight = minHeight;
        savedMaxHeight = maxHeight;

        material.SetFloat(MinHeight, minHeight);
        material.SetFloat(MaxHeight, maxHeight);
        material.SetTexture(Colors, colors);
    }
}
