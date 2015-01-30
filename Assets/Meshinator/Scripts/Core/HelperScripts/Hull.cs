/*
 * Meshinator
 * Copyright Mike Mahoney 2013
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MIConvexHull;

public class Hull
{
	#region Properties & Fields
	
	private const int c_MinTrianglesPerImpact = 48;
	
	private const float c_CompressionResistance = 0.95f; // MUST be < 1 and > 0

	// information about how object should break
	private int fractureLayers;
	private int objectGrain;
	
	// Mesh Information
	private List<Vector3> m_Vertices;
	private List<Vector3> m_Normals;
	private List<Vector4> m_Tangents;
	private List<Vector2> m_Uvs;
	private List<int> m_Triangles;
	private List<List<Vector3>> innerVertices = new List<List<Vector3>>();
	private List<List<int>> innerTriangles = new List<List<int>>();
	
	// New SubHulls for storing fracture information
	private SubHull m_FirstSubHull;
	private SubHull m_SecondSubHull;

	#endregion Properties & Fields
	
	#region Constructors
	
	public Hull(Mesh mesh, int layers, int grain)
	{
		// set fracture info
		fractureLayers = layers;
		objectGrain = grain;

		// Get all the mesh information
		m_Vertices = new List<Vector3>(mesh.vertices);
		m_Triangles = new List<int>(mesh.triangles);
		
		if (mesh.normals.Length > 0)
			m_Normals = new List<Vector3>(mesh.normals);
		
		if (mesh.tangents.Length > 0)
			m_Tangents = new List<Vector4>(mesh.tangents);
		
		if (mesh.uv.Length > 0)
			m_Uvs = new List<Vector2>(mesh.uv);
	}
	
	#endregion Constructors
	
	#region Deformation Functions

	public void Impact(Vector3 impactPoint, Vector3 impactForce, Meshinator.ImpactShapes impactShape, Meshinator.ImpactTypes impactType, List<Vector3> fractureVertices)
	{
		// Look through all triangles to see which ones are within the impactForce from the impactPoint,
		// and measure the area of every triangle in the list
		Dictionary<int, float> triangleIndexToTriangleArea = new Dictionary<int, float>();
		foreach (int triangleIndex in GetIntersectedTriangleIndices(impactPoint, impactForce.magnitude))
		{
			float areaOfTriangle = GetAreaOfTriangle(triangleIndex);
			triangleIndexToTriangleArea.Add(triangleIndex, areaOfTriangle);
		}

		// Keep breaking down the largest triangle until there are more than c_MinTrianglesPerImpact
		// triangles in the list
		while (triangleIndexToTriangleArea.Keys.Count < c_MinTrianglesPerImpact)
		{
			// If we have 64988 vertices or more, we can't add any more or we risk going over the
			// 65000 limit, which causes problems for unity.
			if (m_Vertices.Count > 64988)
				break;
			
			// Get the index of the biggest triangle in our dictionary
			int indexOfLargestTriangle = GetIndexOfLargestTriangle(triangleIndexToTriangleArea);

			// Break that triangle down and remove it from the dictionary
			List<int> newTriangleIndices = BreakDownTriangle(indexOfLargestTriangle, impactPoint, impactForce, fractureVertices);
			triangleIndexToTriangleArea.Remove(indexOfLargestTriangle);
			
			// Measure the areas of the resulting triangles, and add them to the dictionary
			foreach (int triangleIndex in newTriangleIndices)
			{
				// Make sure each triangle is still intersected by our force before we add it back to the list
				if (IsTriangleIndexIntersected(triangleIndex, impactPoint, impactForce.magnitude))
				{
					float areaOfTriangle = GetAreaOfTriangle(triangleIndex);
					triangleIndexToTriangleArea.Add(triangleIndex, areaOfTriangle);
				}
			}
		}
	}
	
	private List<int> GetIntersectedTriangleIndices(Vector3 impactPoint, float impactRadius)
	{
		List<int> intersectedTriangles = new List<int>();
		for (int i = 0; i < m_Triangles.Count; i = i + 3)
		{
			if (IsTriangleIndexIntersected(i, impactPoint, impactRadius))
				intersectedTriangles.Add(i);	
		}
		
		return intersectedTriangles;
	}
	
	private bool IsTriangleIndexIntersected(int triangleIndex, Vector3 impactPoint, float impactRadius)
	{
		// Make sure we've got a good triangle index
		if (triangleIndex % 3 != 0)
		{
			Debug.LogError("Invalid Triangle index: " + triangleIndex + "  Must be a multiple of 3!");
			return false;
		}
		
		// Get the vectors for our triangle
		Vector3 A = m_Vertices[m_Triangles[triangleIndex]] - impactPoint;
		Vector3 B = m_Vertices[m_Triangles[triangleIndex + 1]] - impactPoint;
		Vector3 C = m_Vertices[m_Triangles[triangleIndex + 2]] - impactPoint;
		
		// Is the impact sphere outside the triangle plane?
		float rr = impactRadius * impactRadius;
		Vector3 V = Vector3.Cross(B - A, C - A);
		float d = Vector3.Dot(A, V);
		float e = Vector3.Dot(V, V);
		bool sep1 = d * d > rr * e;
		if (sep1)
			return false;
		
		// Is the impact sphere outside a triangle vertex?
		float aa = Vector3.Dot(A, A);
		float ab = Vector3.Dot(A, B);
		float ac = Vector3.Dot(A, C);
		float bb = Vector3.Dot(B, B);
		float bc = Vector3.Dot(B, C);
		float cc = Vector3.Dot(C, C);
		bool sep2 = (aa > rr) && (ab > aa) && (ac > aa);
		bool sep3 = (bb > rr) && (ab > bb) && (bc > bb);
		bool sep4 = (cc > rr) && (ac > cc) && (bc > cc);
		if (sep2 || sep3 || sep4)
			return false;
		
		// Is the impact sphere outside a triangle edge?
		Vector3 AB = B - A;
		Vector3 BC = C - B;
		Vector3 CA = A - C;
		float d1 = ab - aa;
		float d2 = bc - bb;
		float d3 = ac - cc;
		float e1 = Vector3.Dot(AB, AB);
		float e2 = Vector3.Dot(BC, BC);
		float e3 = Vector3.Dot(CA, CA);
		Vector3 Q1 = AB * e1 - d1 * AB;
		Vector3 Q2 = BC * e2 - d2 * BC;
		Vector3 Q3 = CA * e3 - d3 * CA;
		Vector3 QC = C * e1 - Q1;
		Vector3 QA = A * e2 - Q2;
		Vector3 QB = B * e3 - Q3;
		bool sep5 = (Vector3.Dot(Q1, Q1) > rr * e1 * e1) && (Vector3.Dot(Q1, QC) > 0);
		bool sep6 = (Vector3.Dot(Q2, Q2) > rr * e2 * e2) && (Vector3.Dot(Q2, QA) > 0);
		bool sep7 = (Vector3.Dot(Q3, Q3) > rr * e3 * e3) && (Vector3.Dot(Q3, QB) > 0);
		if (sep5 || sep6 || sep7)
			return false;
		
		// If we've gotten here, then this impact force DOES intersect this triangle.
		return true;
	}

	private bool IsVertexIntersected(Vector3 vertex, Vector3 impactPoint, float impactRadius)
	{
		float farthestX = impactPoint.x + impactRadius;
		float leastX = impactPoint.x - impactRadius;
		float farthestY = impactPoint.y + impactRadius;
		float leastY = impactPoint.y - impactRadius;
		float farthestZ = impactPoint.z + impactRadius;
		float leastZ = impactPoint.z - impactRadius;
		if ((vertex.x > farthestX || vertex.z < leastX) || 
			(vertex.y > farthestY || vertex.y < leastY) || 
			(vertex.z > farthestZ || vertex.x < leastZ)) {
				return false;
		}
		
		// If we've gotten here, then this impact force DOES intersect this triangle.
		return true;
	}
	
	private List<int> BreakDownTriangle(int triangleIndex, Vector3 impactPoint, Vector3 impactForce, List<Vector3> fractureVertices)
	{
		List<int> newTriangleIndices = new List<int>();
		newTriangleIndices.Add(triangleIndex);
		
		// If we have 64988 vertices or more, we can't add any more or we risk going over the
		// 65000 limit, which causes problems for unity.
		if (m_Vertices.Count > 64988)
			return newTriangleIndices;
		
		// Get the vertex indices and store them here
		int indexA = m_Triangles[triangleIndex];
		int indexB = m_Triangles[triangleIndex + 1];
		int indexC = m_Triangles[triangleIndex + 2];

		// Create list of vertices that will be used in this method
		List<Vector3> newVertices = new List<Vector3>();
		
		// Get the 3 vertices for this triangle
		Vector3 vertexA = m_Vertices[indexA];
		Vector3 vertexB = m_Vertices[indexB];
		Vector3 vertexC = m_Vertices[indexC];
		newVertices.Add(vertexA);
		newVertices.Add(vertexB);
		newVertices.Add(vertexC);

		// Find the center points of this triangle sides. We'll be adding these as a new vertices.
		Vector3 centerAB = (vertexA + vertexB) / 2f;
		Vector3 centerAC = (vertexA + vertexC) / 2f;
		Vector3 centerBC = (vertexB + vertexC) / 2f;
		newVertices.Add(centerAB);
		newVertices.Add(centerAC);
		newVertices.Add(centerBC);

		// Adjust the old triangle to use one of the new vertices
		m_Vertices.Add(centerAB);
		m_Vertices.Add(centerAC);
		m_Triangles[triangleIndex + 1] = m_Vertices.Count - 2;
		m_Triangles[triangleIndex + 2] = m_Vertices.Count - 1;
		
		// Add 3 new vertices for the other triangles
		m_Vertices.Add(centerAC);
		m_Vertices.Add(centerBC);
		m_Vertices.Add(vertexC);
		m_Triangles.Add(m_Vertices.Count - 2);
		m_Triangles.Add(m_Vertices.Count - 1);
		m_Triangles.Add(m_Vertices.Count - 3);
		newTriangleIndices.Add(m_Triangles.Count - 3);
		
		// Add 3 new vertices for the other triangles
		m_Vertices.Add(centerAB);
		m_Vertices.Add(centerBC);
		m_Vertices.Add(vertexB);
		m_Triangles.Add(m_Vertices.Count - 1);
		m_Triangles.Add(m_Vertices.Count - 2);
		m_Triangles.Add(m_Vertices.Count - 3);
		newTriangleIndices.Add(m_Triangles.Count - 3);
	
		// Add 3 new vertices for the other triangles
		m_Vertices.Add(centerAB);
		m_Vertices.Add(centerBC);
		m_Vertices.Add(centerAC);
		m_Triangles.Add(m_Vertices.Count - 2);
		m_Triangles.Add(m_Vertices.Count - 1);
		m_Triangles.Add(m_Vertices.Count - 3);
		newTriangleIndices.Add(m_Triangles.Count - 3);
		
		// Add new normals. These MUST be added in the same order as the vertices above!
		if (m_Normals.Count > 0)
		{
			Vector3 normalA = m_Normals[indexA];
			Vector3 normalB = m_Normals[indexB];
			Vector3 normalC = m_Normals[indexC];
			Vector3 normalAB = (normalA + normalB) / 2;
			Vector3 normalAC = (normalA + normalC) / 2;
			Vector3 normalBC = (normalB + normalC) / 2;
			
			m_Normals.Add(normalAB);
			m_Normals.Add(normalAC);
			m_Normals.Add(normalAC);
			m_Normals.Add(normalBC);
			m_Normals.Add(normalC);
			m_Normals.Add(normalAB);
			m_Normals.Add(normalBC);
			m_Normals.Add(normalB);
			m_Normals.Add(normalAB);
			m_Normals.Add(normalBC);
			m_Normals.Add(normalAC);
		}
		
		// Add new tangents. These MUST be added in the same order as the vertices above!
		if (m_Tangents.Count > 0)
		{
			Vector4 tangentA = m_Tangents[indexA];
			Vector4 tangentB = m_Tangents[indexB];
			Vector4 tangentC = m_Tangents[indexC];
			Vector4 tangentAB = (tangentA + tangentB) / 2;
			Vector4 tangentAC = (tangentA + tangentC) / 2;
			Vector4 tangentBC = (tangentB + tangentC) / 2;
			
			m_Tangents.Add(tangentAB);
			m_Tangents.Add(tangentAC);
			m_Tangents.Add(tangentAC);
			m_Tangents.Add(tangentBC);
			m_Tangents.Add(tangentC);
			m_Tangents.Add(tangentAB);
			m_Tangents.Add(tangentBC);
			m_Tangents.Add(tangentB);
			m_Tangents.Add(tangentAB);
			m_Tangents.Add(tangentBC);
			m_Tangents.Add(tangentAC);
		}
		
		// Add new UVs. These MUST be added in the same order as the vertices above!
		if (m_Uvs.Count > 0)
		{
			Vector2 uvA = m_Uvs[indexA];
			Vector2 uvB = m_Uvs[indexB];
			Vector2 uvC = m_Uvs[indexC];
			Vector2 uvAB = (uvA + uvB) / 2;
			Vector2 uvAC = (uvA + uvC) / 2;
			Vector2 uvBC = (uvB + uvC) / 2;
			
			m_Uvs.Add(uvAB);
			m_Uvs.Add(uvAC);
			m_Uvs.Add(uvAC);
			m_Uvs.Add(uvBC);
			m_Uvs.Add(uvC);
			m_Uvs.Add(uvAB);
			m_Uvs.Add(uvBC);
			m_Uvs.Add(uvB);
			m_Uvs.Add(uvAB);
			m_Uvs.Add(uvBC);
			m_Uvs.Add(uvAC);
		}
		
		// check if all vertices are in the impact zone
		if (IsTriangleImpacted(vertexA, vertexB, vertexC, impactPoint)) {
			if (IsVertexIntersected(vertexA, impactPoint, impactForce.magnitude) &&
			    IsVertexIntersected(vertexB, impactPoint, impactForce.magnitude) && 
			    IsVertexIntersected(vertexC, impactPoint, impactForce.magnitude)) {

					AddInnerVertices(newVertices, fractureVertices);
			}
		}
		
		return newTriangleIndices;
	}

	private bool IsTriangleImpacted(Vector3 vertexA, Vector3 vertexB, Vector3 vertexC, Vector3 impactPoint) {
		double xIndex = System.Math.Round(impactPoint.x, 1);
		double yIndex = System.Math.Round(impactPoint.y, 1);
		double zIndex = System.Math.Round(impactPoint.z, 1);
		if ((vertexA.z == zIndex && vertexB.z == zIndex && vertexC.z == zIndex) ||
		    (vertexA.x == xIndex && vertexB.x == xIndex && vertexC.x == xIndex) ||
		    (vertexA.y == yIndex && vertexB.y == yIndex && vertexC.y == yIndex)) {
			return true;
		} else {
			return false;
		}
	}

	// Duplicate all vertices of the given triangle, but move them back the given depth. Return the shifted vertices so they can be used for more shifting.
	private void AddInnerVertices(List<Vector3> newVertices, List<Vector3> fractureVertices) {
		// Get original vertices
		Vector3 vertexA = newVertices[0];
		Vector3 vertexB = newVertices[1];
		Vector3 vertexC = newVertices[2];
		Vector3 vertexAB = newVertices[3];
		Vector3 vertexAC = newVertices[4];
		Vector3 vertexBC = newVertices[5];

		foreach (Vector3 fracture in fractureVertices) {
			// create the 4 shards leading to each fracture vector
			createShardClockwise(vertexA, vertexB, vertexC, fracture);
			//createShardClockwise(vertexAB, vertexB, vertexBC, fracture);
			//createShardClockwise(vertexAC, vertexBC, vertexC, fracture);
			//createShardCounterClockwise(vertexAC, vertexAB, vertexBC, fracture);
		}
	}

	private void createPrismClockwise(Vector3 vertexA, Vector3 vertexB, Vector3 vertexC, Vector3 vertexA1, Vector3 vertexB1, Vector3 vertexC1) {
		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();
		
		// add original face
		vertices.Add(vertexA);
		vertices.Add(vertexB);
		vertices.Add(vertexC);
		triangles.Add(vertices.Count - 3);
		triangles.Add(vertices.Count - 2);
		triangles.Add(vertices.Count - 1);
		
		// add shifted face
		vertices.Add(vertexA1);
		vertices.Add(vertexB1);
		vertices.Add(vertexC1);
		triangles.Add(vertices.Count - 1);
		triangles.Add(vertices.Count - 2);
		triangles.Add(vertices.Count - 3);
		
		// add sides
		vertices.Add(vertexA);
		vertices.Add(vertexA1);
		vertices.Add(vertexB);
		triangles.Add(vertices.Count - 3);
		triangles.Add(vertices.Count - 2);
		triangles.Add(vertices.Count - 1);
		
		vertices.Add(vertexA1);
		vertices.Add(vertexB);
		vertices.Add(vertexB1);
		triangles.Add(vertices.Count - 3);
		triangles.Add(vertices.Count - 1);
		triangles.Add(vertices.Count - 2);
		
		vertices.Add(vertexB);
		vertices.Add(vertexB1);
		vertices.Add(vertexC);
		triangles.Add(vertices.Count - 3);
		triangles.Add(vertices.Count - 2);
		triangles.Add(vertices.Count - 1);
		
		vertices.Add(vertexB1);
		vertices.Add(vertexC);
		vertices.Add(vertexC1);
		triangles.Add(vertices.Count - 2);
		triangles.Add(vertices.Count - 3);
		triangles.Add(vertices.Count - 1);
		
		vertices.Add(vertexA);
		vertices.Add(vertexA1);
		vertices.Add(vertexC);
		triangles.Add(vertices.Count - 3);
		triangles.Add(vertices.Count - 1);
		triangles.Add(vertices.Count - 2);
		
		vertices.Add(vertexA1);
		vertices.Add(vertexC);
		vertices.Add(vertexC1);
		triangles.Add(vertices.Count - 2);
		triangles.Add(vertices.Count - 1);
		triangles.Add(vertices.Count - 3);
		
		innerVertices.Add(vertices);
		innerTriangles.Add(triangles);
	}
	
	private void createPrismCounterClockwise(Vector3 vertexA, Vector3 vertexB, Vector3 vertexC, Vector3 vertexA1, Vector3 vertexB1, Vector3 vertexC1) {
		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();
		
		// add original face
		vertices.Add(vertexA);
		vertices.Add(vertexB);
		vertices.Add(vertexC);
		triangles.Add(vertices.Count - 1);
		triangles.Add(vertices.Count - 2);
		triangles.Add(vertices.Count - 3);
		
		// add shifted face
		vertices.Add(vertexA1);
		vertices.Add(vertexB1);
		vertices.Add(vertexC1);
		triangles.Add(vertices.Count - 3);
		triangles.Add(vertices.Count - 2);
		triangles.Add(vertices.Count - 1);
		
		// add sides
		vertices.Add(vertexA);
		vertices.Add(vertexA1);
		vertices.Add(vertexB);
		triangles.Add(vertices.Count - 1);
		triangles.Add(vertices.Count - 2);
		triangles.Add(vertices.Count - 3);
		
		vertices.Add(vertexA1);
		vertices.Add(vertexB);
		vertices.Add(vertexB1);
		triangles.Add(vertices.Count - 1);
		triangles.Add(vertices.Count - 3);
		triangles.Add(vertices.Count - 2);
		
		vertices.Add(vertexB);
		vertices.Add(vertexB1);
		vertices.Add(vertexC);
		triangles.Add(vertices.Count - 1);
		triangles.Add(vertices.Count - 2);
		triangles.Add(vertices.Count - 3);
		
		vertices.Add(vertexB1);
		vertices.Add(vertexC);
		vertices.Add(vertexC1);
		triangles.Add(vertices.Count - 2);
		triangles.Add(vertices.Count - 1);
		triangles.Add(vertices.Count - 3);
		
		vertices.Add(vertexA);
		vertices.Add(vertexA1);
		vertices.Add(vertexC);
		triangles.Add(vertices.Count - 1);
		triangles.Add(vertices.Count - 3);
		triangles.Add(vertices.Count - 2);
		
		vertices.Add(vertexA1);
		vertices.Add(vertexC);
		vertices.Add(vertexC1);
		triangles.Add(vertices.Count - 2);
		triangles.Add(vertices.Count - 3);
		triangles.Add(vertices.Count - 1);
		
		innerVertices.Add(vertices);
		innerTriangles.Add(triangles);
	}

	private void createShardClockwise(Vector3 vertexA, Vector3 vertexB, Vector3 vertexC, Vector3 vertexA1) {
		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();

		// add original face
		vertices.Add(vertexA);
		vertices.Add(vertexB);
		vertices.Add(vertexC);
		triangles.Add(vertices.Count - 3);
		triangles.Add(vertices.Count - 2);
		triangles.Add(vertices.Count - 1);
		
		// add sides
		vertices.Add(vertexA);
		vertices.Add(vertexA1);
		vertices.Add(vertexB);
		triangles.Add(vertices.Count - 3);
		triangles.Add(vertices.Count - 2);
		triangles.Add(vertices.Count - 1);
		
		vertices.Add(vertexA1);
		vertices.Add(vertexB);
		vertices.Add(vertexC);
		triangles.Add(vertices.Count - 3);
		triangles.Add(vertices.Count - 1);
		triangles.Add(vertices.Count - 2);
		
		vertices.Add(vertexA);
		vertices.Add(vertexA1);
		vertices.Add(vertexC);
		triangles.Add(vertices.Count - 3);
		triangles.Add(vertices.Count - 1);
		triangles.Add(vertices.Count - 2);
		
		innerVertices.Add(vertices);
		innerTriangles.Add(triangles);
	}

	private void createShardCounterClockwise(Vector3 vertexA, Vector3 vertexB, Vector3 vertexC, Vector3 vertexA1) {
		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();
		
		// add original face
		vertices.Add(vertexA);
		vertices.Add(vertexB);
		vertices.Add(vertexC);
		triangles.Add(vertices.Count - 1);
		triangles.Add(vertices.Count - 2);
		triangles.Add(vertices.Count - 3);
		
		// add sides
		vertices.Add(vertexA);
		vertices.Add(vertexA1);
		vertices.Add(vertexB);
		triangles.Add(vertices.Count - 1);
		triangles.Add(vertices.Count - 2);
		triangles.Add(vertices.Count - 3);
		
		vertices.Add(vertexA1);
		vertices.Add(vertexB);
		vertices.Add(vertexC);
		triangles.Add(vertices.Count - 1);
		triangles.Add(vertices.Count - 3);
		triangles.Add(vertices.Count - 2);
		
		vertices.Add(vertexA);
		vertices.Add(vertexA1);
		vertices.Add(vertexC);
		triangles.Add(vertices.Count - 1);
		triangles.Add(vertices.Count - 3);
		triangles.Add(vertices.Count - 2);
		
		innerVertices.Add(vertices);
		innerTriangles.Add(triangles);
	}

	public List<Mesh> GetInnerMeshes() {
		List<Mesh> innerMeshes = new List<Mesh>();
		for (int i=0; i < innerVertices.Count; i++) {
			Mesh innerMesh = new Mesh();
			innerMesh.vertices = innerVertices[i].ToArray();
			innerMesh.triangles = innerTriangles[i].ToArray();
			innerMesh.RecalculateNormals();
			innerMesh.RecalculateBounds();
			innerMeshes.Add(innerMesh);
		}
		return innerMeshes;
	}
	
	private float GetAreaOfTriangle(int triangleIndex)
	{
		// Get the vertices of the triangle
		Vector3 vertexA = m_Vertices[m_Triangles[triangleIndex]];
		Vector3 vertexB = m_Vertices[m_Triangles[triangleIndex + 1]];
		Vector3 vertexC = m_Vertices[m_Triangles[triangleIndex + 2]];
		
		// Figure out the area of the triangle
		Vector3 v = Vector3.Cross(vertexA - vertexB, vertexA - vertexC);
		return v.magnitude * 0.5f;
	}
	
	private int GetIndexOfLargestTriangle(Dictionary<int, float> triangleIndexToTriangleArea)
	{
		int indexOfLargestTriangle = 0;
		float areaOfLargestTriangle = 0f;
		foreach (int triangleIndex in triangleIndexToTriangleArea.Keys)
		{
			float areaOfCurrentTriangle = triangleIndexToTriangleArea[triangleIndex];
			if (areaOfCurrentTriangle > areaOfLargestTriangle)
			{
				indexOfLargestTriangle = triangleIndex;
				areaOfLargestTriangle = areaOfCurrentTriangle;
			}
		}
		
		return indexOfLargestTriangle;
	}
	
	public bool IsEmpty()
	{
		return m_Vertices.Count < 3 || m_Triangles.Count < 3;
	}
	
	public Mesh GetMesh(Vector3 impactPoint, float impactRadius, List<Vector3> fractureVertices)
	{
		if (!IsEmpty())
		{
			Mesh mesh = new Mesh();

			// shift all front vertices back
			float shiftDepth = impactRadius * fractureLayers;
			for (int i=0; i < m_Vertices.Count; i++) {
				Vector3 vertex = m_Vertices[i];
				if (IsVertexIntersected(vertex, impactPoint, impactRadius)) {
					m_Vertices[i] = moveToClosestFractureVertex(vertex, fractureVertices);
				}
			}
			
			mesh.vertices = m_Vertices.ToArray();
			mesh.triangles = m_Triangles.ToArray();

			if (m_Normals != null)
				mesh.normals = m_Normals.ToArray();

			if (m_Tangents != null)
				mesh.tangents = m_Tangents.ToArray();
			
			if (m_Uvs != null)
				mesh.uv = m_Uvs.ToArray();

			mesh.RecalculateBounds();
			return mesh;
		}
		
		return null;
	}

	private Vector3 moveToClosestFractureVertex(Vector3 vertex, List<Vector3> fractureVertices) {
		Vector3 closest = vertex;
		float closestDistance = 100.0f;
		foreach (Vector3 fracture in fractureVertices) {
			float distance = Vector3.Distance(vertex, fracture);
			if (distance < closestDistance) {
				closest = fracture;
				closestDistance = distance;
			}
		}
		return closest;
	}
	
	public Mesh GetSubHullMesh()
	{
		if (m_SecondSubHull != null && !m_SecondSubHull.IsEmpty())
			return m_SecondSubHull.GetMesh();
		
		return null;
	}
	
	#endregion Utility Functions
}
