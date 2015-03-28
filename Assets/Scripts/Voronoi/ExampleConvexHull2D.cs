using UnityEngine;
using System.Collections.Generic;
using MIConvexHull;

public class ExampleConvexHull2D : MonoBehaviour 
{
	public int NumberOfVertices = 1000;
	public double size = 5;

	Material lineMaterial;
	Mesh mesh;

	List<Vertex2> convexHullVertices;
	List<Face2> convexHullFaces;

	void CreateLineMaterial() 
	{
		if( !lineMaterial ) 
		{
			lineMaterial = new Material("Shader \"Lines/Colored Blended\" {" +
			                            "SubShader { Pass { " +
			                            "    Blend SrcAlpha OneMinusSrcAlpha " +
			                            "    ZWrite Off Cull Off Fog { Mode Off } " +
			                            "    BindChannels {" +
			                            "      Bind \"vertex\", vertex Bind \"color\", color }" +
			                            "} } }" );
			
			lineMaterial.hideFlags = HideFlags.HideAndDontSave;
			lineMaterial.shader.hideFlags = HideFlags.HideAndDontSave;
		}
	}

	// Use this for initialization
	void Start () 
	{
		CreateLineMaterial();

		mesh = new Mesh();
		Vertex2[] vertices = new Vertex2[NumberOfVertices];
		Vector3[] meshVerts = new Vector3[NumberOfVertices];
		int[] indices = new int[NumberOfVertices];

		Random.seed = 0;
		for (var i = 0; i < NumberOfVertices; i++)
		{
			vertices[i] = new Vertex2(size * Random.Range(-1.0f, 1.0f), size * Random.Range(-1.0f, 1.0f));
			meshVerts[i] = vertices[i].ToVector3();
			indices[i] = i;
		}

		mesh.vertices = meshVerts;
		mesh.SetIndices(indices, MeshTopology.Points, 0);
		//mesh.bounds = new Bounds(Vector3.zero, new Vector3((float)size,(float)size,(float)size));

		float now = Time.realtimeSinceStartup;
		ConvexHull<Vertex2, Face2> convexHull = ConvexHull.Create<Vertex2, Face2>(vertices);
		float interval = Time.realtimeSinceStartup - now;

		convexHullVertices = new List<Vertex2>(convexHull.Points);
		convexHullFaces = new List<Face2>(convexHull.Faces);

		Debug.Log("Out of the " + NumberOfVertices + " vertices, there are " + convexHullVertices.Count + " verts on the convex hull.");
		Debug.Log("time = " + interval * 1000.0f + " ms");

	}

	void Update()
	{
		Graphics.DrawMesh(mesh, Matrix4x4.identity, lineMaterial, 0, Camera.main);
	}

	void OnPostRender() 
	{
		GL.PushMatrix();
		
		GL.LoadIdentity();
		GL.MultMatrix(GetComponent<Camera>().worldToCameraMatrix);
		GL.LoadProjectionMatrix(GetComponent<Camera>().projectionMatrix);
		
		lineMaterial.SetPass( 0 );
		GL.Begin( GL.LINES );
		GL.Color( Color.red );

		foreach(Face2 f in convexHullFaces)
		{
			GL.Vertex3( (float)f.Vertices[0].x, (float)f.Vertices[0].y, 0.0f);
			GL.Vertex3( (float)f.Vertices[1].x, (float)f.Vertices[1].y, 0.0f);
		}
		
		GL.End();
		
		GL.PopMatrix();
	}
}



















