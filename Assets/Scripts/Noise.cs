// Noise.cs
//
// Description:
// Implements a fractal Perlin noise generator for procedural terrain generation.
// Uses multiple octaves of noise to create natural-looking heightmaps with
// configurable detail levels and seamless tiling support.
//
// Key Features:
// - Generates 2D noise maps using Unity's Perlin noise
// - Supports multiple octaves for fractal noise
// - Provides both local and global value normalization
// - Handles infinite terrain through sample center offsetting
// - Maintains consistency through seeded randomization
//
// Parameters Explained:
// - Scale: Controls the zoom level of the noise
//   * Lower values = smaller terrain features
//   * Higher values = larger terrain features
//
// - Octaves: Number of noise layers combined
//   * More octaves = more detail but higher computation cost
//   * Each octave adds finer details at higher frequencies
//
// - Persistence: Controls how quickly amplitudes diminish
//   * Range: [0,1]
//   * Higher values = more pronounced detail layers
//   * Lower values = smoother terrain
//
// - Lacunarity: Controls frequency increase per octave
//   * Typically 2.0 (each octave doubles frequency)
//   * Higher values = more rapid detail addition

using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Static class providing noise generation functionality for terrain generation.
/// </summary>
public static class Noise {
	/// <summary>
	/// Defines how the noise values should be normalized.
	/// Local: Normalizes based on the actual min/max values in the generated noise map.
	/// Global: Normalizes based on the theoretical min/max values possible with the current settings.
	/// </summary>
	public enum NormalizeMode {Local, Global};

	/// <summary>
	/// Generates a 2D noise map using Perlin noise.
	/// </summary>
	/// <param name="mapWidth">Width of the noise map</param>
	/// <param name="mapHeight">Height of the noise map</param>
	/// <param name="settings">Noise generation parameters</param>
	/// <param name="sampleCenter">Center point for sampling, used for infinite terrain generation</param>
	/// <returns>2D array of normalized noise values</returns>
	public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCenter) 
	{
		var noiseMap = new float[mapWidth,mapHeight];

		System.Random prng = new System.Random(settings.seed);
		var octaveOffsets = new Vector2[settings.octaves];

		float maxPossibleHeight = 0;
		float amplitude = 1;
		float frequency = 1;

		// Generate random offsets for each octave
		for (int i = 0; i < settings.octaves; i++) 
		{
			float offsetX = prng.Next(-100000, 100000) + settings.offset.x + sampleCenter.x;
			float offsetY = prng.Next(-100000, 100000) - settings.offset.y - sampleCenter.y;
			octaveOffsets [i] = new Vector2 (offsetX, offsetY);

			maxPossibleHeight += amplitude;
			amplitude *= settings.persistence;
		}

		float maxLocalNoiseHeight = float.MinValue;
		float minLocalNoiseHeight = float.MaxValue;

		float halfWidth = mapWidth / 2f;
		float halfHeight = mapHeight / 2f;
		
		// Generate noise values
		for (int y = 0; y < mapHeight; y++) 
		{
			for (int x = 0; x < mapWidth; x++) 
			{
				amplitude = 1;
				frequency = 1;
				float noiseHeight = 0;

				// Accumulate noise from each octave
				for (int i = 0; i < settings.octaves; i++) 
				{
					float sampleX = (x-halfWidth + octaveOffsets[i].x) / settings.scale * frequency;
					float sampleY = (y-halfHeight + octaveOffsets[i].y) / settings.scale * frequency;

					// Generate base noise value and transform from [0,1] to [-1,1]
					float perlinValue = Mathf.PerlinNoise (sampleX, sampleY) * 2 - 1;
					noiseHeight += perlinValue * amplitude;

					amplitude *= settings.persistence;
					frequency *= settings.lacunarity;
				}

				maxLocalNoiseHeight = Mathf.Max(maxLocalNoiseHeight, noiseHeight);
				minLocalNoiseHeight = Mathf.Min(minLocalNoiseHeight, noiseHeight);
				noiseMap [x, y] = noiseHeight;

				if (settings.normalizeMode == NormalizeMode.Global)
				{
					float normalizedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight / 0.9f);
					noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
				}
			}
		}

		if (settings.normalizeMode == NormalizeMode.Local)
		{
			for (int y = 0; y < mapHeight; y++)
			{
				for (int x = 0; x < mapWidth; x++)
				{
					noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
				}
			}
		}

		return noiseMap;
	}
}

/// <summary>
/// Contains all settings required for noise generation.
/// Serializable for Unity inspector integration.
/// </summary>
[Serializable]
public class NoiseSettings
{
	/// <summary>
	/// Determines how the noise values should be normalized
	/// </summary>
	public Noise.NormalizeMode normalizeMode;

	/// <summary>
	/// Scale of the noise. Larger values create larger features.
	/// </summary>
	public float scale = 50f;

	/// <summary>
	/// Number of noise layers to combine. More octaves = more detail but more expensive.
	/// </summary>
	public int octaves = 6;

	/// <summary>
	/// How much each octave contributes to the overall shape (amplitude modifier per octave)
	/// </summary>
	[Range(0, 1)]
	public float persistence = 0.6f;
	
	/// <summary>
	/// How much detail is added by each octave (frequency modifier per octave)
	/// </summary>
	public float lacunarity = 2f;

	/// <summary>
	/// Seed for random number generation. Same seed = same terrain.
	/// </summary>
	public int seed;

	/// <summary>
	/// 2D offset for noise sampling position
	/// </summary>
	public Vector2 offset;

	/// <summary>
	/// Ensures all noise settings are within valid ranges
	/// </summary>
	public void ValidateValues()
	{
		scale = Mathf.Max(scale, 0.01f);
		octaves = Mathf.Max(octaves, 1);
		persistence = Mathf.Clamp01(persistence);
		lacunarity = Mathf.Max(lacunarity, 1);
	}
}