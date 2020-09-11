using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MakeDumbMesh : MonoBehaviour
{
	public Mesh mesh;

	[Range(2, 400)]
	public int PointCountThousand = 100;
	public int PointCount { get { return PointCountThousand * 1000; } }
	public int VertexCount { get { return PointCount * 3; } }

	void GenerateMesh()
	{
		//	modify existing asset where possible
		mesh = MakeMesh(PointCount, mesh);
		//	try and make the user save it as a file
#if UNITY_EDITOR
		mesh = AssetWriter.SaveAsset(mesh);
#endif
		var mf = GetComponent<MeshFilter>();
		if (mf != null)
		{
			mf.sharedMesh = mesh;
		}
	}

	void Update()
	{
		//	auto regen mesh
		if (mesh != null)
		{
			if (VertexCount != mesh.vertexCount)
				GenerateMesh();
		}
	}

	void OnEnable()
	{
		Update();

		if (mesh == null)
			GenerateMesh();
	}

	public static void AddTriangle(ref List<Vector3> Positions, ref List<int> Indexes, int Index)
	{
		//	xy = local triangle uv
		//	z = triangle index
		//	gr: if we have all triangles in the same place, this causes huge overdraw and cripples the GPU when it tries to render the raw mesh.
		//	gr: change this so it matches web layout (doesnt really matter, but might as well)
		var pos0 = new Vector3(0, 0, Index);
		var pos1 = new Vector3(1, 0, Index);
		var pos2 = new Vector3(0, 1, Index);

		var VertexIndex = Positions.Count;

		Positions.Add(pos0);
		Positions.Add(pos1);
		Positions.Add(pos2);

		Indexes.Add(VertexIndex + 0);
		Indexes.Add(VertexIndex + 1);
		Indexes.Add(VertexIndex + 2);
	}

	public static Mesh MakeMesh(int TriangleCount,Mesh ExistingMesh)
	{
		Debug.Log("Generating new mesh x" + TriangleCount);
		var Positions = new List<Vector3>();
		var Indexes = new List<int>();

		for (int i = 0; i < TriangleCount; i++)
		{
			AddTriangle(ref Positions,ref Indexes, i);
		}

		//	create new mesh if we need to
		if (ExistingMesh == null )
			ExistingMesh = new Mesh();
		var Mesh = ExistingMesh;
		Mesh.name = "Triangle Mesh x" + TriangleCount;

		Mesh.SetVertices(Positions);

		Mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		Mesh.SetIndices(Indexes.ToArray(), MeshTopology.Triangles, 0, true );

		return Mesh;
	}
}
