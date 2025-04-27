// MeshGenerator.cs
//
// Description: Provides functionality for generating terrain meshes with LOD support
// 
// Key Features:
// - Generates terrain meshes from height maps
// - Supports multiple LOD levels for performance optimization
// - Handles both flat and smooth shading
// - Manages seamless LOD transitions with out-of-mesh vertices
// - Provides UV mapping for texture application

using UnityEngine;

/// <summary>
/// Handles the generation of terrain meshes with Level of Detail (LOD) support.
/// Works in conjunction with TerrainChunk system for efficient terrain rendering.
/// </summary>
public static class MeshGenerator 
{
	/// <summary>
    /// Generates a terrain mesh with LOD support from a height map.
    /// </summary>
    /// <param name="heightMap">2D array of height values</param>
    /// <param name="meshSettings">Configuration for mesh generation</param>
    /// <param name="levelOfDetail">LOD level (0 = highest detail)</param>
	public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail) 
	{
		// Calculate vertex skip rate based on LOD level
        // LOD0 = every vertex, LOD1 = every 2nd vertex, LOD2 = every 4th vertex, etc.
		int skipIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
		int numVerticesPerLine = meshSettings.NumVerticesPerLine;
		Vector2 topLeft = new Vector2(-1, 1) * meshSettings.MeshWorldSize / 2f;

		var meshData = new MeshData(numVerticesPerLine, skipIncrement, meshSettings.useFlatShading);

		// First pass: Create vertex index mapping
		// This map helps track which vertices are actually used in the mesh
		var vertexIndicesMap = new int[numVerticesPerLine, numVerticesPerLine];
		int meshVertexIndex = 0;
		int outOfMeshVertexIndex = -1;

		// Map vertex indices and identify which vertices to skip based on LOD
		for (int y = 0; y < numVerticesPerLine; y++) 
		{
			for (int x = 0; x < numVerticesPerLine; x++) 
			{
				// Vertices on the mesh border are treated specially for LOD transitions
				bool isOutOfMeshVertex = y == 0 || y == numVerticesPerLine - 1 || x == 0 || x == numVerticesPerLine - 1;
				// Skip vertices based on LOD level, but keep border vertices
				bool isSkippedVertex = x > 2 && x < numVerticesPerLine - 3 && y > 2 && y < numVerticesPerLine - 3 && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);

				if (isOutOfMeshVertex) 
				{
					vertexIndicesMap[x, y] = outOfMeshVertexIndex;
					outOfMeshVertexIndex--;
				} 
				else if (!isSkippedVertex)
				{
					vertexIndicesMap[x, y] = meshVertexIndex;
					meshVertexIndex++;
				}
			}
		}

		// Second pass: Generate vertices and triangles
		for (int y = 0; y < numVerticesPerLine; y++) 
		{
			for (int x = 0; x < numVerticesPerLine; x++) 
			{
				// Skip vertices that aren't used in this LOD level
				bool isSkippedVertex = x > 2 && x < numVerticesPerLine - 3 && y > 2 && y < numVerticesPerLine - 3 &&
					((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);

				if (isSkippedVertex)
				{
					continue;
				}

				bool isOutOfMeshVertex = y == 0 || y == numVerticesPerLine - 1 || x == 0 || x == numVerticesPerLine - 1;
				bool isMeshEdgeVertex = (y == 1 || y == numVerticesPerLine - 2 || x == 1 || x == numVerticesPerLine - 2) && !isOutOfMeshVertex;
				bool isMainVertex = (x - 2) % skipIncrement == 0 && (y - 2) % skipIncrement == 0 && !isOutOfMeshVertex && !isMeshEdgeVertex;
				bool isEdgeConnectionVertex =
					(y == 2 || y == numVerticesPerLine - 3 || x == 2 || x == numVerticesPerLine - 3) && !isOutOfMeshVertex && !isMeshEdgeVertex && !isMainVertex;
				
				int vertexIndex = vertexIndicesMap [x, y];
				Vector2 percent = new Vector2(x - 1, y -1) / (numVerticesPerLine - 3);
				Vector2 vertexPosition2d = topLeft + new Vector2(percent.x, -percent.y) * meshSettings.MeshWorldSize;
				float height = heightMap[x, y];

				// Handle edge connection vertices specially to ensure smooth LOD transitions
				if (isEdgeConnectionVertex)
				{
					// Interpolate height between main vertices for smooth LOD transitions
					bool isVertical = x == 2 || x == numVerticesPerLine - 3;
					int dstToMainVertexA = (isVertical ? (y - 2) : (x - 2)) % skipIncrement;
					int dstToMainVertexB = skipIncrement - dstToMainVertexA;
					float dstPercentFromAToB = dstToMainVertexA / (float)skipIncrement;

					// Get heights of main vertices to interpolate between
					float heightMainVertexA = heightMap[(isVertical ? x : x - dstToMainVertexA), (isVertical ? y - dstToMainVertexA : y)];
					float heightMainVertexB = heightMap[(isVertical ? x : x + dstToMainVertexB), (isVertical ? y + dstToMainVertexB : y)];

					height = heightMainVertexA * (1 - dstPercentFromAToB) + heightMainVertexB * dstPercentFromAToB;
				}
				
				meshData.AddVertex(new Vector3(vertexPosition2d.x, height, vertexPosition2d.y), percent, vertexIndex);

				// Create triangles, skipping edge connection vertices at chunk borders
				bool createTriangle = x < numVerticesPerLine - 1 && y < numVerticesPerLine - 1 && (!isEdgeConnectionVertex || (x != 2 && y != 2));
				
				if (createTriangle)
				{
					int currentIncrement = (isMainVertex && x != numVerticesPerLine - 3 && y != numVerticesPerLine - 3) ? skipIncrement : 1;
					
					int a = vertexIndicesMap[x, y];
					int b = vertexIndicesMap[x + currentIncrement, y];
					int c = vertexIndicesMap[x, y + currentIncrement];
					int d = vertexIndicesMap[x + currentIncrement, y + currentIncrement];

					meshData.AddTriangle(a,d,c);
					meshData.AddTriangle(d,a,b);
				}
			}
		}

		meshData.ProcessMesh();

		return meshData;
	}
}

/// <summary>
/// Container for mesh data during generation process.
/// Handles both regular mesh vertices and additional vertices needed for LOD transitions.
/// </summary>
public class MeshData 
{
	/// <summary>
    /// Array of vertex positions in 3D space
    /// </summary>
	private Vector3[] vertices;

	/// <summary>
    /// Array of triangle indices defining mesh topology
    /// </summary>
	private int[] triangles;

	/// <summary>
    /// Array of UV coordinates for texture mapping
    /// </summary>
	private Vector2[] uvs;

	/// <summary>
    /// Array of baked normals for smooth shading
    /// </summary>
	private Vector3[] bakedNormals;

	/// <summary>
    /// Array of vertices that lie outside the main mesh, used for LOD transitions
    /// </summary>
	private Vector3[] outOfMeshVertices;

	/// <summary>
    /// Array of triangle indices for out-of-mesh geometry
    /// </summary>
	private int[] outOfMeshTriangles;

	/// <summary>
    /// Current index for adding triangles to the mesh
    /// </summary>
	private int triangleIndex;

	/// <summary>
    /// Current index for adding triangles to the out-of-mesh geometry
    /// </summary>
	private int outOfMeshTriangleIndex;

	/// <summary>
    /// Flag indicating if flat shading is used
    /// </summary>
	private bool useFlatShading;

	/// <summary>
    /// Initializes a new instance of MeshData with specified dimensions and shading settings.
    /// </summary>
    /// <param name="numVerticesPerLine">Number of vertices per row/column in the mesh grid</param>
    /// <param name="skipIncrement">Vertex skip value for LOD calculation</param>
    /// <param name="useFlatShading">Whether to use flat shading instead of smooth shading</param>
	public MeshData(int numVerticesPerLine, int skipIncrement, bool useFlatShading) 
	{
		this.useFlatShading = useFlatShading;

		int numMeshEdgeVertices = (numVerticesPerLine - 2) * 4 - 4;
		int numEdgeConnectionVertices = (skipIncrement - 1) * (numVerticesPerLine - 5) / skipIncrement * 4;
		int numMainVerticesPerLine = (numVerticesPerLine - 5) / skipIncrement + 1;
		int numMainVertices = numMainVerticesPerLine * numMainVerticesPerLine;

		vertices = new Vector3[numMeshEdgeVertices + numEdgeConnectionVertices + numMainVertices];
		uvs = new Vector2[vertices.Length];

		int numMeshEdgeTriangles = 8 * (numVerticesPerLine - 4);
		int numMainTriangles = (numMainVerticesPerLine - 1) * (numMainVerticesPerLine - 1) * 2;
		triangles = new int[(numMeshEdgeTriangles + numMainTriangles) * 3];

		outOfMeshVertices = new Vector3[numVerticesPerLine * 4 - 4];
		outOfMeshTriangles = new int[24 * (numVerticesPerLine - 2)];
	}

	/// <summary>
    /// Adds a vertex to the mesh data structure.
    /// </summary>
    /// <param name="vertexPosition">Position of the vertex in 3D space</param>
    /// <param name="uv">UV coordinates for texture mapping</param>
    /// <param name="vertexIndex">Index of the vertex (negative for out-of-mesh vertices)</param>
	public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex) 
	{
		if (vertexIndex < 0) 
		{
			outOfMeshVertices[-vertexIndex - 1] = vertexPosition;
		} 
		else 
		{
			vertices[vertexIndex] = vertexPosition;
			uvs[vertexIndex] = uv;
		}
	}

	/// <summary>
    /// Adds a triangle to the mesh by specifying three vertex indices.
    /// </summary>
    /// <param name="a">Index of first vertex</param>
    /// <param name="b">Index of second vertex</param>
    /// <param name="c">Index of third vertex</param>
	public void AddTriangle(int a, int b, int c) 
	{
		if (a < 0 || b < 0 || c < 0) 
		{
			outOfMeshTriangles[outOfMeshTriangleIndex] = a;
			outOfMeshTriangles[outOfMeshTriangleIndex + 1] = b;
			outOfMeshTriangles[outOfMeshTriangleIndex + 2] = c;
			outOfMeshTriangleIndex += 3;
		} 
		else 
		{
			triangles[triangleIndex] = a;
			triangles[triangleIndex + 1] = b;
			triangles[triangleIndex + 2] = c;
			triangleIndex += 3;
		}
	}

	/// <summary>
    /// Calculates normal vectors for all vertices in the mesh.
    /// </summary>
    /// <returns>Array of normal vectors corresponding to each vertex</returns>
	private Vector3[] CalculateNormals() 
	{

		var vertexNormals = new Vector3[vertices.Length];
		int triangleCount = triangles.Length / 3;
		for (int i = 0; i < triangleCount; i++) 
		{
			int normalTriangleIndex = i * 3;
			int vertexIndexA = triangles[normalTriangleIndex];
			int vertexIndexB = triangles[normalTriangleIndex + 1];
			int vertexIndexC = triangles[normalTriangleIndex + 2];

			Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
			vertexNormals[vertexIndexA] += triangleNormal;
			vertexNormals[vertexIndexB] += triangleNormal;
			vertexNormals[vertexIndexC] += triangleNormal;
		}

		int borderTriangleCount = outOfMeshTriangles.Length / 3;
		for (int i = 0; i < borderTriangleCount; i++) 
		{
			int normalTriangleIndex = i * 3;
			int vertexIndexA = outOfMeshTriangles[normalTriangleIndex];
			int vertexIndexB = outOfMeshTriangles[normalTriangleIndex + 1];
			int vertexIndexC = outOfMeshTriangles[normalTriangleIndex + 2];

			Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
			if (vertexIndexA >= 0) 
			{
				vertexNormals [vertexIndexA] += triangleNormal;
			}
			if (vertexIndexB >= 0) 
			{
				vertexNormals [vertexIndexB] += triangleNormal;
			}
			if (vertexIndexC >= 0) 
			{
				vertexNormals [vertexIndexC] += triangleNormal;
			}
		}

		for (int i = 0; i < vertexNormals.Length; i++) 
		{
			vertexNormals [i].Normalize();
		}

		return vertexNormals;
	}

	/// <summary>
    /// Calculates the surface normal for a triangle defined by three vertex indices.
    /// </summary>
    /// <param name="indexA">Index of first vertex</param>
    /// <param name="indexB">Index of second vertex</param>
    /// <param name="indexC">Index of third vertex</param>
    /// <returns>Normalized surface normal vector</returns>
	private Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC) {
		Vector3 pointA = (indexA < 0) ? outOfMeshVertices[-indexA-1] : vertices[indexA];
		Vector3 pointB = (indexB < 0) ? outOfMeshVertices[-indexB-1] : vertices[indexB];
		Vector3 pointC = (indexC < 0) ? outOfMeshVertices[-indexC-1] : vertices[indexC];

		Vector3 sideAB = pointB - pointA;
		Vector3 sideAC = pointC - pointA;
		return Vector3.Cross(sideAB, sideAC).normalized;
	}

	/// <summary>
    /// Processes the mesh data by either applying flat shading or calculating smooth normals.
    /// Should be called after all vertices and triangles have been added.
    /// </summary>
	public void ProcessMesh() 
	{
		if (useFlatShading) 
		{
			FlatShading();
		} 
		else 
		{
			BakeNormals();
		}
	}

	/// <summary>
    /// Calculates and stores smooth normal vectors for all vertices in the mesh.
    /// </summary>
	private void BakeNormals() 
	{
		bakedNormals = CalculateNormals ();
	}

	/// <summary>
    /// Converts the mesh to use flat shading by duplicating vertices at triangle edges.
    /// </summary>
	private void FlatShading() 
	{
		var flatShadedVertices = new Vector3[triangles.Length];
		var flatShadedUvs = new Vector2[triangles.Length];

		for (int i = 0; i < triangles.Length; i++) 
		{
			flatShadedVertices[i] = vertices[triangles [i]];
			flatShadedUvs[i] = uvs[triangles [i]];
			triangles[i] = i;
		}

		vertices = flatShadedVertices;
		uvs = flatShadedUvs;
	}

	/// <summary>
    /// Creates and returns a Unity Mesh object from the stored mesh data.
    /// </summary>
    /// <returns>A new Unity Mesh object with vertices, triangles, UVs, and normals</returns>
	public Mesh CreateMesh() 
	{
		var mesh = new Mesh
		{
			vertices = vertices,
			triangles = triangles,
			uv = uvs
		};
		
		if (useFlatShading) 
		{
			mesh.RecalculateNormals();
		} 
		else 
		{
			mesh.normals = bakedNormals;
		}
		
		return mesh;
	}
}