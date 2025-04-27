// HeightMapSettings.cs
//
// Description:
// Scriptable Object that defines the configuration for terrain height map generation.
// Combines noise settings, height modification, and falloff controls to create
// customizable terrain height distributions. This asset can be created and modified
// in the Unity Editor for real-time terrain adjustment.
//
// Key Features:
// - Noise parameter configuration
// - Height curve customization
// - Falloff toggle support
// - Height range control
// - Editor-time validation

using UnityEngine;

/// <summary>
/// Scriptable Object containing settings for terrain height map generation.
/// Create via: Create > Scriptable Objects > HeightMapSettings
/// </summary>
[CreateAssetMenu(fileName = "HeightMapSettings", menuName = "Scriptable Objects/HeightMapSettings")]
public class HeightMapSettings : UpdatableData
{
    /// <summary>
    /// Settings controlling the noise generation for terrain patterns
    /// </summary>
    public NoiseSettings noiseSettings;

    /// <summary>
    /// Whether to apply edge falloff to the terrain
    /// </summary>
    public bool useFalloff;

    /// <summary>
    /// Global multiplier for terrain height
    /// </summary>
    public float heightMultiplier;

    /// <summary>
    /// Curve defining height distribution across the terrain.
    /// X-axis: normalized height value (0-1)
    /// Y-axis: height multiplier
    /// </summary>
    public AnimationCurve heightCurve;

    /// <summary>
    /// Calculated minimum possible height for current settings
    /// </summary>
    public float MinHeight => heightMultiplier * heightCurve.Evaluate(0);

    /// <summary>
    /// Calculated maximum possible height for current settings
    /// </summary>
    public float MaxHeight => heightMultiplier * heightCurve.Evaluate(1);
    
    #if UNITY_EDITOR
    /// <summary>
    /// Validates settings when changed in the Unity Editor.
    /// Only compiled in editor, not in builds.
    /// </summary>
    protected override void OnValidate()
    {
        noiseSettings.ValidateValues();
        base.OnValidate();
    }
    #endif
}
