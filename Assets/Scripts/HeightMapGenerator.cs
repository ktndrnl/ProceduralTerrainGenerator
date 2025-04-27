// HeightMapGenerator.cs
//
// Description:
// Generates height maps for terrain generation by combining noise data with
// height curve modifications. This class serves as a crucial bridge between
// raw noise generation and usable terrain height data, providing thread-safe
// height map generation for the procedural terrain system.
//
// Key Features:
// - Thread-safe height map generation
// - Height curve application for terrain shaping
// - Automatic min/max value tracking
// - Scale-aware sampling
// - Configurable height multiplier

using UnityEngine;

/// <summary>
/// Generates height maps for terrain generation with thread-safe processing.
/// </summary>
public static class HeightMapGenerator
{
    /// <summary>
    /// Generates a height map by combining noise data with height curve modifications.
    /// </summary>
    /// <param name="width">Width of the height map</param>
    /// <param name="height">Height of the height map</param>
    /// <param name="settings">Height map generation settings including noise and curve data</param>
    /// <param name="sampleCenter">Center point for noise sampling, used for infinite terrain</param>
    /// <returns>HeightMap structure containing height data and value ranges</returns>
    public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCenter)
    {
        // Generate base noise map
        float[,] values = Noise.GenerateNoiseMap(width, height, settings.noiseSettings, sampleCenter);

        // generate a thread-safe copy of the height curve as GenerateHeightMap is called from multiple threads
        var heightCurveThreadsafe = new AnimationCurve(settings.heightCurve.keys);
        
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;
        
        // Apply height curve and track value ranges
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                values[i, j] *= heightCurveThreadsafe.Evaluate(values[i, j]) * settings.heightMultiplier;
                
                maxValue = Mathf.Max(maxValue, values[i, j]);
                minValue = Mathf.Min(minValue, values[i, j]);
            }
        }
        
        return new HeightMap(values, minValue, maxValue);
    }
}

/// <summary>
/// Immutable structure containing height map data and its value range.
/// Thread-safe and suitable for parallel terrain generation.
/// </summary>
public readonly struct HeightMap
{
    /// <summary>
    /// 2D array of height values
    /// </summary>
    public readonly float[,] Values;

    /// <summary>
    /// Minimum height value in the map
    /// </summary>
    public readonly float MinValue;

    /// <summary>
    /// Maximum height value in the map
    /// </summary>
    public readonly float MaxValue;

    /// <summary>
    /// Initializes a new height map with the specified values and range.
    /// </summary>
    /// <param name="values">2D array of height values</param>
    /// <param name="minValue">Minimum height value</param>
    /// <param name="maxValue">Maximum height value</param>
    public HeightMap(float[,] values, float minValue, float maxValue)
    {
        Values = values;
        MinValue = minValue;
        MaxValue = maxValue;
    }
}