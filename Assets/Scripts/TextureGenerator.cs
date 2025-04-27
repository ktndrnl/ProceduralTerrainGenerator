using UnityEngine;

/// <summary>
/// Utility class for generating visualization textures from height map data.
/// </summary>
public static class TextureGenerator
{
    /// <summary>
    /// Creates a texture from a color map array with specific dimensions.
    /// </summary>
    /// <param name="colorMap">Array of colors representing pixel data</param>
    /// <param name="width">Width of the texture</param>
    /// <param name="height">Height of the texture</param>
    /// <returns>A new Texture2D configured for visualization</returns>
    private static Texture2D TextureFromColourMap(Color[] colorMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        
        texture.filterMode = FilterMode.Point;  // No interpolation between pixels
        texture.wrapMode = TextureWrapMode.Clamp;  // No texture wrapping
        
        // Set pixel data and apply changes
        texture.SetPixels(colorMap);
        texture.Apply();
        
        return texture;
    }

    /// <summary>
    /// Generates a grayscale visualization texture from a height map.
    /// Black represents minimum height, white represents maximum height.
    /// </summary>
    /// <param name="heightMap">Height map data containing elevation values and range</param>
    /// <returns>A grayscale Texture2D representing the height map</returns>
    public static Texture2D TextureFromHeightMap(HeightMap heightMap)
    {
        int width = heightMap.Values.GetLength(0);
        int height = heightMap.Values.GetLength(1);

        var colourMap = new Color[width * height];
        
        // Convert height values to grayscale colors
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Normalize height value and convert to grayscale
                colourMap[y * width + x] = Color.Lerp(
                    Color.black, 
                    Color.white, 
                    Mathf.InverseLerp(heightMap.MinValue, heightMap.MaxValue, heightMap.Values[x, y])
                );
            }
        }

        return TextureFromColourMap(colourMap, width, height);
    }
}
