// FalloffGenerator.cs
//
// Description:
// Generates a falloff map for terrain edge smoothing, creating natural-looking
// boundaries for terrain chunks. Uses a customized mathematical function to
// create smooth transitions from terrain to void, preventing abrupt cutoffs
// at terrain edges.

using UnityEngine;

/// <summary>
/// Generates falloff maps for terrain edge smoothing using a modified sigmoid function.
/// </summary>
public static class FalloffGenerator
{
    /// <summary>
    /// Generates a square falloff map of specified size.
    /// </summary>
    /// <param name="size">Width and height of the falloff map (should match terrain chunk size)</param>
    /// <returns>2D array of falloff values between 0 and 1</returns>
    public static float[,] GenerateFalloffMap(int size)
    {
        var map = new float[size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                // Convert coordinates to [-1,1] range
                float x = i / (float)size * 2 - 1;
                float y = j / (float)size * 2 - 1;

                // Use maximum of absolute coordinates for radial falloff
                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                map[i, j] = Evaluate(value);
            }
        }

        return map;
    }

    /// <summary>
    /// Evaluates the falloff function for a given value.
    /// Uses a modified sigmoid function: f(x) = x^a / (x^a + (b-bx)^a)
    /// </summary>
    /// <param name="value">Input value, typically in range [0,1]</param>
    /// <returns>Falloff value between 0 and 1</returns>
    private static float Evaluate(float value)
    {
        // a controls the steepness of the falloff
        const float a = 3;
        // b controls where the falloff starts to take effect
        const float b = 2.2f;

        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}
