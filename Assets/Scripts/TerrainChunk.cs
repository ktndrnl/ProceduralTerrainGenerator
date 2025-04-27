// TerrainChunk.cs
//
// Description:
// Represents and manages a single chunk of terrain in an infinite procedural terrain system.
// Each chunk handles its own lifecycle, LOD (Level of Detail) management, mesh generation,
// and visibility culling based on viewer distance. This is a core component that works in
// conjunction with TerrainGenerator to create a seamless, infinite terrain experience.

using System;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Manages a single chunk of terrain with LOD support and visibility culling.
/// </summary>
public class TerrainChunk
{
	/// <summary>
	/// Event triggered when chunk visibility changes. Parameters are the chunk and its new visibility state.
	/// </summary>
	public event Action<TerrainChunk, bool> OnVisibilityChanged;

	/// <summary>
	/// World space coordinates of this chunk
	/// </summary>
	public Vector2 coord;

	/// <summary>
	/// Minimum distance threshold for generating collision meshes
	/// </summary>
	private const float ColliderGenerationDistanceThreshold = 5f;

	// Unity Components
	private GameObject meshObject;
	private Vector2 sampleCenter;
	private Bounds bounds;
	private MeshRenderer meshRenderer;
	private MeshFilter meshFilter;
	private MeshCollider meshCollider;
	private GameObject waterObject;

	// LOD Management
	private LODInfo[] detailLevels;
	private LODMesh[] lodMeshes;
	private int colliderLODIndex;
	private int previousLODIndex = -1;

	// State Tracking
	private HeightMap heightMap;
	private bool heightMapReceived;
	private bool hasSetCollider;
	private float maxViewDistance;

	// Settings and References
	private HeightMapSettings heightMapSettings;
	private MeshSettings meshSettings;
	private Transform viewer;
	private Vector2 ViewerPosition => new Vector2(viewer.position.x, viewer.position.z);

	/// <summary>
	/// Initializes a new terrain chunk with specified settings and position.
	/// </summary>
	/// <param name="coord">World space coordinates for this chunk</param>
	/// <param name="heightMapSettings">Height map generation settings</param>
	/// <param name="meshSettings">Mesh generation settings</param>
	/// <param name="detailLevels">Array of LOD configurations</param>
	/// <param name="colliderLODIndex">Which LOD level to use for collision</param>
	/// <param name="parent">Parent transform for this chunk</param>
	/// <param name="viewer">Transform to use for LOD calculations</param>
	/// <param name="material">Material to apply to the terrain</param>
	public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, WaterSettings waterSettings, 
		LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material material)
	{
		this.coord = coord;
		this.detailLevels = detailLevels;
		this.colliderLODIndex = colliderLODIndex;
		this.heightMapSettings = heightMapSettings;
		this.meshSettings = meshSettings;
		this.viewer = viewer;

		sampleCenter = coord * meshSettings.MeshWorldSize / meshSettings.meshScale;
		Vector2 position = coord * meshSettings.MeshWorldSize;
		bounds = new Bounds(position,Vector2.one * meshSettings.MeshWorldSize);

		meshObject = new GameObject("Terrain Chunk");
		meshRenderer = meshObject.AddComponent<MeshRenderer>();
		meshFilter = meshObject.AddComponent<MeshFilter>();
		meshCollider = meshObject.AddComponent<MeshCollider>();
		meshRenderer.material = material;

		meshObject.transform.position = new Vector3(position.x, 0, position.y);
		meshObject.transform.parent = parent;

		waterObject = Object.Instantiate(waterSettings.waterPrefab, meshObject.transform, false);
		SetVisible(false);

		lodMeshes = new LODMesh[detailLevels.Length];
		for (int i = 0; i < detailLevels.Length; i++) 
		{
			lodMeshes[i] = new LODMesh(detailLevels[i].lod);
			lodMeshes[i].UpdateCallback += UpdateTerrainChunk;
			if (i == colliderLODIndex)
			{
				lodMeshes[i].UpdateCallback += UpdateCollisionMesh;
			}
		}

		maxViewDistance = detailLevels[^1].visibleDstThreshold;
	}

	/// <summary>
	/// Initiates asynchronous loading of height map data for this chunk.
	/// </summary>
	public void Load()
	{
		ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(meshSettings.NumVerticesPerLine,
			meshSettings.NumVerticesPerLine,
			heightMapSettings, sampleCenter), OnHeightMapReceived);
	}

	public void DestroyChunk()
	{
		if (waterObject != null)
		{
			Object.Destroy(waterObject);
		}
		Object.Destroy(meshFilter.mesh);
		Object.Destroy(meshFilter.sharedMesh);
		foreach (LODMesh lodMesh in lodMeshes)
		{
			Object.Destroy(lodMesh.mesh);
		}

		Object.Destroy(meshRenderer);
	}
	
	private void OnHeightMapReceived(object heightMapObject) 
	{
		heightMap = (HeightMap)heightMapObject;
		heightMapReceived = true;

		UpdateTerrainChunk();
	}

	/// <summary>
	/// Updates chunk visibility and LOD based on viewer distance.
	/// Should be called when viewer position changes significantly.
	/// </summary>
	public void UpdateTerrainChunk()
	{
		if (!heightMapReceived)
		{
			return;
		}
		
		float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance (ViewerPosition));
		bool wasVisible = IsVisible;
		bool visible = viewerDstFromNearestEdge <= maxViewDistance;

		if (visible) 
		{
			int lodIndex = 0;

			for (int i = 0; i < detailLevels.Length - 1; i++) 
			{
				if (viewerDstFromNearestEdge > detailLevels [i].visibleDstThreshold) 
				{
					lodIndex = i + 1;
				} 
				else 
				{
					break;
				}
			}

			if (lodIndex != previousLODIndex) 
			{
				LODMesh lodMesh = lodMeshes [lodIndex];
				if (lodMesh.hasMesh) 
				{
					previousLODIndex = lodIndex;
					meshFilter.mesh = lodMesh.mesh;
				} 
				else if (!lodMesh.hasRequestedMesh) 
				{
					lodMesh.RequestMesh(heightMap, meshSettings);
				}
			}
		}

		if (wasVisible != visible)
		{
			SetVisible(visible);
			OnVisibilityChanged?.Invoke(this, visible);
		}
	}

	/// <summary>
	/// Updates collision mesh based on viewer distance.
	/// Generates collision only when viewer is within threshold distance.
	/// </summary>
	public void UpdateCollisionMesh()
	{
		if (hasSetCollider)
		{
			return;
		}
		
		float sqrDistanceFromViewerToEdge = bounds.SqrDistance(ViewerPosition);

		if (sqrDistanceFromViewerToEdge < detailLevels[colliderLODIndex].visibleDstThreshold)
		{
			if (!lodMeshes[colliderLODIndex].hasRequestedMesh)
			{
				lodMeshes[colliderLODIndex].RequestMesh(heightMap, meshSettings);
			}
		}
		
		if (sqrDistanceFromViewerToEdge < ColliderGenerationDistanceThreshold * ColliderGenerationDistanceThreshold)
		{
			if (lodMeshes[colliderLODIndex].hasMesh)
			{
				meshCollider.sharedMesh = lodMeshes[colliderLODIndex].mesh;
				hasSetCollider = true;
			}
		}
	}

	private void SetVisible(bool visible) 
	{
		meshObject.SetActive (visible);
	}

	private bool IsVisible => meshObject.activeSelf;
}

/// <summary>
/// Manages mesh data for a specific LOD level, including async loading.
/// </summary>
public class LODMesh 
{
	public Mesh mesh;
	public bool hasRequestedMesh;
	public bool hasMesh;
	private int lod;
	public event Action UpdateCallback;

	public LODMesh(int lod) 
	{
		this.lod = lod;
	}

	private void OnMeshDataReceived(object meshDataObject) 
	{
		mesh = ((MeshData)meshDataObject).CreateMesh();
		hasMesh = true;

		UpdateCallback?.Invoke();
	}

	public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings) 
	{
		hasRequestedMesh = true;
		ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.Values, meshSettings, lod), OnMeshDataReceived);
	}
}

/// <summary>
/// Configuration data for a single LOD level.
/// </summary>
[Serializable]
public struct LODInfo
{
	[Range(0, MeshSettings.NumSupportedLODs - 1)]
	public int lod;

	public float visibleDstThreshold;
	public bool useForCollider;
	public float SqrVisibleDistanceThreshold => visibleDstThreshold * visibleDstThreshold;
}