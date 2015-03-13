using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MIConvexHull;

public class ExampleDelaunayAndVoronoi2D : MonoBehaviour 
{

	public int NumberOfVertices = 1000;
	public float size = 5.0f;
	
	Material lineMaterial;
	Mesh mesh;

	List<Vertex2> vertices;
	VoronoiMesh<Vertex2, Cell2, VoronoiEdge<Vertex2, Cell2>> voronoiMesh;

	bool drawVoronoi = true;
	bool drawDelaunay = false;
	bool drawGhostVerts = false;
	
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
		voronoiMesh = VoronoiMesh.Create<Vertex2, Cell2>(vertices);
		float interval = Time.realtimeSinceStartup - now;

		Debug.Log("time = " + interval * 1000.0f);
		
	}
	
	void Update()
	{
		if(Input.GetKeyDown(KeyCode.F1)) drawVoronoi = !drawVoronoi;
		if(Input.GetKeyDown(KeyCode.F2)) drawDelaunay = !drawDelaunay;
		if(Input.GetKeyDown(KeyCode.F3)) drawGhostVerts = !drawGhostVerts;

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

		if(drawVoronoi)
		{
			foreach(var edge in voronoiMesh.Edges)
			{
				bool draw = true;

				if(!drawGhostVerts)
				{
					if(edge.Source.Circumcenter.x > size || edge.Source.Circumcenter.x < -size) draw = false;
					if(edge.Target.Circumcenter.x > size || edge.Target.Circumcenter.x < -size) draw = false;

					if(edge.Source.Circumcenter.y > size || edge.Source.Circumcenter.y < -size) draw = false;
					if(edge.Target.Circumcenter.y > size || edge.Target.Circumcenter.y < -size) draw = false;

					if(!draw) continue;
				}

				GL.Vertex3( edge.Source.Circumcenter.x, edge.Source.Circumcenter.y, 0.0f);
				GL.Vertex3( edge.Target.Circumcenter.x, edge.Target.Circumcenter.y, 0.0f);
			}
		}

		GL.Color( Color.blue );

		if(drawDelaunay)
		{
			foreach (var cell in voronoiMesh.Vertices)
			{

				GL.Vertex3( (float)cell.Vertices[0].x, (float)cell.Vertices[0].y, 0.0f);
				GL.Vertex3( (float)cell.Vertices[1].x, (float)cell.Vertices[1].y, 0.0f);

				GL.Vertex3( (float)cell.Vertices[0].x, (float)cell.Vertices[0].y, 0.0f);
				GL.Vertex3( (float)cell.Vertices[2].x, (float)cell.Vertices[2].y, 0.0f);

				GL.Vertex3( (float)cell.Vertices[2].x, (float)cell.Vertices[2].y, 0.0f);
				GL.Vertex3( (float)cell.Vertices[1].x, (float)cell.Vertices[1].y, 0.0f);
			}
		}

		GL.End();
		
		GL.PopMatrix();
	}
}



















