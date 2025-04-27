// MapPreview.cs
//
// Description:
// Editor utility class for previewing different aspects of the procedural terrain
// system in real-time within the Unity Editor. Provides visual feedback for
// noise maps, mesh generation, and falloff maps, essential for terrain design
// and debugging.
//
// Preview Modes:
// 1. Noise Map:
//    - Displays raw height map data as 2D texture
//    - Shows terrain elevation distribution
//    - Useful for tweaking noise parameters
//
// 2. Mesh Preview:
//    - Shows actual 3D terrain mesh
//    - Supports LOD preview
//    - Displays terrain with materials
//
// 3. Falloff Map:
//    - Visualizes edge falloff calculations
//    - Helps tune terrain boundaries
//    - Shows gradient from edge to center

using System;
using UnityEngine;

/// <summary>
/// Editor utility for previewing different aspects of the procedural terrain system.
/// </summary>
public class MapPreview : MonoBehaviour
{
    /// <summary>
    /// Renderer for 2D texture previews (noise and falloff maps)
    /// </summary>
    public Renderer textureRender;

    /// <summary>
    /// Filter for 3D mesh previews
    /// </summary>
    public MeshFilter meshFilter;

    /// <summary>
    /// Renderer for 3D mesh previews
    /// </summary>
    public MeshRenderer meshRenderer;
    
    /// <summary>
    /// Current preview visualization mode
    /// </summary>
    public DrawMode drawMode;

    /// <summary>
    /// Available preview visualization modes
    /// </summary>
    public enum DrawMode { NoiseMap, Mesh, FalloffMap };

    /// <summary>
    /// Settings for mesh generation
    /// </summary>
    public MeshSettings meshSettings;

    /// <summary>
    /// Settings for height map generation
    /// </summary>
    public HeightMapSettings heightMapSettings;

    /// <summary>
    /// Settings for terrain texturing
    /// </summary>
    public TextureData textureData;

    /// <summary>
    /// Material used for terrain visualization
    /// </summary>
    public Material terrainMaterial;

    /// <summary>
    /// LOD level to preview in editor
    /// </summary>
    [Range(0, MeshSettings.NumSupportedLODs - 1)]
    public int editorPreviewLOD;

    /// <summary>
    /// Whether to automatically update preview when parameters change
    /// </summary>
    public bool autoUpdate;

    /// <summary>
    /// Generates and displays the map preview based on current settings
    /// </summary>
    public void DrawMapInEditor() 
    {
        // Apply texture settings and generate height map
        textureData.ApplyToMaterial(terrainMaterial);
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(
            meshSettings.NumVerticesPerLine, 
            meshSettings.NumVerticesPerLine, 
            heightMapSettings, 
            Vector2.zero
        );

        switch (drawMode)
        {
            case DrawMode.NoiseMap:
                DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
                break;
            case DrawMode.Mesh:
                DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.Values, meshSettings, editorPreviewLOD));
                break;
            case DrawMode.FalloffMap:
                DrawTexture(TextureGenerator.TextureFromHeightMap(
                    new HeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.NumVerticesPerLine), 0, 1)
                ));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void OnValidate() 
    {
    	if (meshSettings != null) 
    	{
    		meshSettings.OnValuesUpdated -= OnValuesUpdated;
    		meshSettings.OnValuesUpdated += OnValuesUpdated;
    	}
    	if (heightMapSettings != null) 
    	{
    		heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
    		heightMapSettings.OnValuesUpdated += OnValuesUpdated;
    	}
    	if (textureData != null) 
    	{
    		textureData.OnValuesUpdated -= OnTextureValuesUpdated;
    		textureData.OnValuesUpdated += OnTextureValuesUpdated;
    	}
    }

	private void DrawTexture(Texture2D texture)
    {
        textureRender.sharedMaterial.mainTexture = texture;
        textureRender.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10f;
		
		textureRender.gameObject.SetActive(true);
		meshFilter.gameObject.SetActive(false);
    }

	private void DrawMesh(MeshData meshData)
    {
        meshFilter.sharedMesh = meshData.CreateMesh();

		textureRender.gameObject.SetActive(false);
		meshFilter.gameObject.SetActive(true);
    }

	private void OnValuesUpdated()
	{
		if (!Application.isPlaying)
		{
			DrawMapInEditor();
		}

		textureData.ApplyToMaterial(terrainMaterial);
	}

	private void OnTextureValuesUpdated()
	{
		textureData.ApplyToMaterial(terrainMaterial);
	}
}
