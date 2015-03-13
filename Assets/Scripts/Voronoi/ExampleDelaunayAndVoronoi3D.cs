using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MIConvexHull;

public class ExampleDelaunayAndVoronoi3D : MonoBehaviour 
{

	public int NumberOfVertices = 200;
	public float size = 5.0f;
	
	Material lineMaterial;
	Mesh mesh;
	
	List<Vertex3> vertices;
	VoronoiMesh<Vertex3, Cell3, VoronoiEdge<Vertex3, Cell3>> voronoiMesh;
	Matrix4x4 rotation = Matrix4x4.identity;

	float theta;

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
		Vertex3[] vertices = new Vertex3[NumberOfVertices];
		Vector3[] meshVerts = new Vector3[NumberOfVertices];
		int[] indices = new int[NumberOfVertices];
		
		Random.seed = 0;
		for (var i = 0; i < NumberOfVertices; i++)
		{
			vertices[i] = new Vertex3(size * Random.Range(-1.0f, 1.0f), size * Random.Range(-1.0f, 1.0f), size * Random.Range(-1.0f, 1.0f));
			meshVerts[i] = vertices[i].ToVector3();
			indices[i] = i;
		}
		
		mesh.vertices = meshVerts;
		mesh.SetIndices(indices, MeshTopology.Points, 0);
		//mesh.bounds = new Bounds(Vector3.zero, new Vector3((float)size,(float)size,(float)size));
		
		float now = Time.realtimeSinceStartup;
		voronoiMesh = VoronoiMesh.Create<Vertex3, Cell3>(vertices);
		float interval = Time.realtimeSinceStartup - now;
		
		Debug.Log("time = " + interval * 1000.0f);
		
	}
	
	void Update()
	{
		if(Input.GetKeyDown(KeyCode.F1)) drawVoronoi = !drawVoronoi;
		if(Input.GetKeyDown(KeyCode.F2)) drawDelaunay = !drawDelaunay;
		if(Input.GetKeyDown(KeyCode.F3)) drawGhostVerts = !drawGhostVerts;

		if(Input.GetKey(KeyCode.KeypadPlus) || Input.GetKey(KeyCode.KeypadMinus))
		{
			theta += (Input.GetKey(KeyCode.KeypadPlus)) ?  0.005f : -0.005f;
		
			rotation[0,0] = Mathf.Cos(theta);
			rotation[0,2] = Mathf.Sin(theta);
			rotation[2,0] = -Mathf.Sin(theta);
			rotation[2,2] = Mathf.Cos(theta);
		}

		Graphics.DrawMesh(mesh, rotation, lineMaterial, 0, Camera.main);
	}
	
	void OnPostRender() 
	{
		GL.PushMatrix();
		
		GL.LoadIdentity();
		GL.MultMatrix(GetComponent<Camera>().worldToCameraMatrix * rotation);
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

					if(edge.Source.Circumcenter.z > size || edge.Source.Circumcenter.z < -size) draw = false;
					if(edge.Target.Circumcenter.z > size || edge.Target.Circumcenter.z < -size) draw = false;
				}
				
				if(!draw) continue;

				GL.Vertex3( edge.Source.Circumcenter.x, edge.Source.Circumcenter.y, edge.Source.Circumcenter.z);
				GL.Vertex3( edge.Target.Circumcenter.x, edge.Target.Circumcenter.y, edge.Target.Circumcenter.z);
			}
		}
		
		GL.Color( Color.blue );

		if(drawDelaunay)
		{
			foreach (var cell in voronoiMesh.Vertices)
			{

				GL.Vertex3( (float)cell.Vertices[0].x, (float)cell.Vertices[0].y, (float)cell.Vertices[0].z);
				GL.Vertex3( (float)cell.Vertices[1].x, (float)cell.Vertices[1].y, (float)cell.Vertices[1].z);
				
				GL.Vertex3( (float)cell.Vertices[0].x, (float)cell.Vertices[0].y, (float)cell.Vertices[0].z);
				GL.Vertex3( (float)cell.Vertices[2].x, (float)cell.Vertices[2].y, (float)cell.Vertices[2].z);

				GL.Vertex3( (float)cell.Vertices[0].x, (float)cell.Vertices[0].y, (float)cell.Vertices[0].z);
				GL.Vertex3( (float)cell.Vertices[3].x, (float)cell.Vertices[3].y, (float)cell.Vertices[3].z);

				GL.Vertex3( (float)cell.Vertices[1].x, (float)cell.Vertices[1].y, (float)cell.Vertices[1].z);
				GL.Vertex3( (float)cell.Vertices[2].x, (float)cell.Vertices[2].y, (float)cell.Vertices[2].z);

				GL.Vertex3( (float)cell.Vertices[1].x, (float)cell.Vertices[1].y, (float)cell.Vertices[1].z);
				GL.Vertex3( (float)cell.Vertices[3].x, (float)cell.Vertices[3].y, (float)cell.Vertices[3].z);

				GL.Vertex3( (float)cell.Vertices[2].x, (float)cell.Vertices[2].y, (float)cell.Vertices[2].z);
				GL.Vertex3( (float)cell.Vertices[3].x, (float)cell.Vertices[3].y, (float)cell.Vertices[3].z);
			}
		}
		
		GL.End();
		
		GL.PopMatrix();
	}
}



















