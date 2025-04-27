// MeshSettings.cs
//
// Description:
// Scriptable Object that defines mesh generation parameters for the procedural
// terrain system. Controls mesh resolution, scaling, LOD settings, and shading
// options. This configuration determines the fundamental geometry characteristics
// of terrain chunks.
//
// Key Features:
// - Configurable mesh resolution
// - LOD system support
// - Flat/smooth shading options
// - Dynamic chunk size selection
// - Automatic border vertex handling

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Scriptable Object containing settings for terrain mesh generation.
/// Create via: Create > Scriptable Objects > MeshSettings
/// </summary>
[CreateAssetMenu(fileName = "MeshSettings", menuName = "Scriptable Objects/MeshSettings")]
public class MeshSettings : UpdatableData
{
    /// <summary>
    /// Maximum number of LOD levels supported by the system
    /// </summary>
    public const int NumSupportedLODs = 5;

    /// <summary>
    /// Number of different chunk size options available
    /// </summary>
    public const int NumSupportedChunkSizes = 9;

    /// <summary>
    /// Maximum LOD levels that support flat shading, less than normal because flat shading uses 3x more vertices
    /// </summary>
    public const int NumSupportedFlatShadedLODs = 3;

    /// <summary>
    /// Available chunk size options for mesh resolution
    /// </summary>
    public static readonly int[] SupportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };
    
    /// <summary>
    /// Base scale factor for the mesh. Affects world-space size of terrain
    /// </summary>
    public float meshScale = 2.5f;

    /// <summary>
    /// Whether to use flat shading for the terrain mesh
    /// </summary>
    public bool useFlatShading;

    /// <summary>
    /// Index into SupportedChunkSizes for mesh resolution
    /// </summary>
    [Range(0, NumSupportedChunkSizes - 1)]
    public int chunkSizeIndex;

    /// <summary>
    /// Chunk size index when using flat shading
    /// </summary>
    [Range(0, NumSupportedFlatShadedLODs - 1)]
    public int flatShadedChunkSizeIndex;

    /// <summary>
    /// Calculates the number of vertices per line including border vertices.
    /// Border vertices are used for normal calculation but not rendered.
    /// </summary>
    public int NumVerticesPerLine => SupportedChunkSizes[(useFlatShading) ? flatShadedChunkSizeIndex : chunkSizeIndex] + 5;
    
    /// <summary>
    /// Calculates the world-space size of the mesh.
    /// Accounts for border vertices in the calculation.
    /// </summary>
    public float MeshWorldSize => (NumVerticesPerLine - 3) * meshScale;
}
