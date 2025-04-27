// TerrainGenerator.cs
//
// Description:
// Core controller for an infinite procedural terrain generation system that dynamically
// creates and manages terrain chunks based on viewer position. Implements a Level of Detail (LOD)
// system for performance optimization and handles seamless terrain loading/unloading.
// 
// Key Features:
// - Infinite terrain generation through chunk-based system
// - Dynamic LOD system with configurable detail levels
// - Threaded terrain generation for performance
// - Automatic chunk loading/unloading based on viewer distance
// - Collision mesh management with LOD support
// - Texture application with height-based blending

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the procedural terrain generation system, handling chunk loading, LOD transitions,
/// and viewer-based terrain updates. This is the main controller class for the terrain system.
/// </summary>
public class TerrainGenerator : MonoBehaviour {
	/// <summary>
	/// Distance the viewer must move before triggering a chunk update.
	/// Helps optimize performance by preventing unnecessary updates.
	/// </summary>
	private const float ViewerMoveThresholdForChunkUpdate = 25f;

	/// <summary>
	/// Squared distance threshold for chunk updates.
	/// Cached to avoid repeated calculations in Update loop.
	/// </summary>
	private const float SqrViewerMoveThresholdForChunkUpdate = ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;

	/// <summary>
	/// Index in the detailLevels array to use for collision meshes.
	/// Lower indices = higher detail collision.
	/// </summary>
	public int colliderLODIndex;

	/// <summary>
	/// Array of LOD settings defining distance thresholds and detail levels.
	/// Index 0 = highest detail, increasing indices = lower detail.
	/// </summary>
	public LODInfo[] detailLevels;

	/// <summary>
	/// Settings controlling mesh generation parameters like size and resolution.
	/// </summary>
	public MeshSettings meshSettings;

	/// <summary>
	/// Settings controlling height map generation including noise parameters.
	/// </summary>
	public HeightMapSettings heightMapSettings;

	/// <summary>
	/// Settings for terrain texturing including color gradients and blend values.
	/// </summary>
	public TextureData textureSettings;
	
	/// <summary>
	/// Settings for water rendering.
	/// </summary>
	public WaterSettings waterSettings;
	
	/// <summary>
	/// Transform of the viewer used for LOD calculations
	/// and chunk loading decisions.
	/// </summary>
	public Transform viewer;

	/// <summary>
	/// Material applied to terrain chunks, updated with texture settings.
	/// </summary>
	public Material mapMaterial;
	
	// Cached viewer position data for optimization
	private Vector2 viewerPosition;
	private Vector2 viewerPositionOld;
	private float meshWorldSize;
	private int chunksVisibleInViewDst;

	/// <summary>
	/// Maps chunk coordinates to TerrainChunk instances.
	/// Allows quick lookup of existing chunks by their position.
	/// </summary>
	private Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new();

	/// <summary>
	/// List of currently visible terrain chunks.
	/// Maintained for efficient updates of visible terrain.
	/// </summary>
	private List<TerrainChunk> visibleTerrainChunks = new();

	private void Start()
	{
		// make copies of settings to avoid modifying original assets
		// meshSettings = Instantiate(meshSettings);
		// heightMapSettings = Instantiate(heightMapSettings);
		// textureSettings = Instantiate(textureSettings);
		// waterSettings = Instantiate(waterSettings);
		
		Initialize();
	}

	private void Initialize()
	{
		// Apply texture settings to the terrain material
		textureSettings.ApplyToMaterial(mapMaterial);
		textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);

		// Calculate view distance based on LOD settings
		float maxViewDst = detailLevels[^1].visibleDstThreshold;
		meshWorldSize = meshSettings.MeshWorldSize;
		chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / meshWorldSize);

		// Initialize visible chunks
		UpdateVisibleChunks();
	}

	/// <summary>
	/// Updates terrain chunks based on viewer movement.
	/// Handles both collision mesh updates and chunk visibility changes.
	/// </summary>
	private void Update() {
		// Update viewer position (only using X and Z coordinates)
		viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

		// Update collision meshes if viewer has moved at all
		if (viewerPosition != viewerPositionOld)
		{
			foreach (TerrainChunk chunk in visibleTerrainChunks)
			{
				chunk.UpdateCollisionMesh();
			}
		}
		
		// Only update chunk visibility if viewer has moved significantly
		if ((viewerPositionOld - viewerPosition).sqrMagnitude > SqrViewerMoveThresholdForChunkUpdate) 
		{
			viewerPositionOld = viewerPosition;
			UpdateVisibleChunks();
		}
	}

	/// <summary>
	/// Updates which chunks should be visible based on viewer position.
	/// Handles creation, updates, and LOD transitions of terrain chunks.
	/// </summary>
	private void UpdateVisibleChunks()
	{
		var alreadyUpdatedChunkCoords = new HashSet<Vector2>();
		
		for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--) 
		{
			alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
			visibleTerrainChunks[i].UpdateTerrainChunk();
		}
			
		int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / meshWorldSize);
		int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / meshWorldSize);

		// Check all potentially visible chunks in view distance
		for (int yOffset = -chunksVisibleInViewDst; yOffset <= chunksVisibleInViewDst; yOffset++) 
		{
			for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++) 
			{
				var viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
				
				if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
				{
					if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
					{
						terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
					}
					else
					{
						var newChunk = new TerrainChunk(viewedChunkCoord, heightMapSettings, meshSettings, waterSettings, detailLevels,
							colliderLODIndex, transform, viewer, mapMaterial);
						terrainChunkDictionary.Add(viewedChunkCoord, newChunk);
						newChunk.OnVisibilityChanged += OnTerrainChunkVisibilityChanged;
						newChunk.Load();
					}
				}
			}
		}
	}

	/// <summary>
	/// Handles visibility changes for terrain chunks.
	/// Maintains the list of currently visible chunks for efficient updates.
	/// </summary>
	/// <param name="chunk">The chunk whose visibility changed</param>
	/// <param name="isVisible">Whether the chunk is now visible</param>
	private void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
	{
		if (isVisible)
		{
			visibleTerrainChunks.Add(chunk);
		}
		else
		{
			visibleTerrainChunks.Remove(chunk);
		}
	}

	public void ResetTerrain()
	{
		if (transform.childCount > 0)
		{
			foreach (KeyValuePair<Vector2, TerrainChunk> pair in terrainChunkDictionary)
			{
				pair.Value.OnVisibilityChanged -= OnTerrainChunkVisibilityChanged;
				pair.Value.DestroyChunk();
			}

			foreach (Component component in transform.GetComponentsInChildren(typeof(Transform), true))
			{
				var childTransform = (Transform)component;
				if (childTransform.gameObject != this.gameObject)
				{
					Destroy(childTransform.gameObject);
				}
			}

			terrainChunkDictionary.Clear();
			visibleTerrainChunks.Clear();
		}
		
		Initialize();
	}
}