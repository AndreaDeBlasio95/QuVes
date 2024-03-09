using CW.Common;
using System.Collections.Generic;
using UnityEngine;

namespace PaintInEditor
{
	/// <summary>This class processes a mesh so it can be painted in more scenarios than it can be default.</summary>
	public static class CwEditorMesh
	{
		private static List<Vector3> oldVertices  = new List<Vector3>();
		private static List<Vector2> oldCoords    = new List<Vector2>();
		private static List<Vector3> oldNormals   = new List<Vector3>();
		private static List<Vector4> oldTangents  = new List<Vector4>();
		private static List<int>     oldTriangles = new List<int>();

		private static List<Vector3> newVertices  = new List<Vector3>();
		private static List<Vector3> newNormals   = new List<Vector3>();
		private static List<Vector4> newTangents  = new List<Vector4>();
		private static List<Vector2> newCoords    = new List<Vector2>();
		private static List<int>     newTriangles = new List<int>();

		public static Mesh GetProcessedCopy(Mesh original, int channel)
		{
			newVertices.Clear();
			newNormals.Clear();
			newTangents.Clear();
			newCoords.Clear();
			newTriangles.Clear();

			original.GetVertices(oldVertices);
			original.GetNormals(oldNormals);
			original.GetTangents(oldTangents);
			original.GetUVs(channel, oldCoords);
			original.GetTriangles(oldTriangles, 0);

			if (oldCoords.Count > 0)
			{
				for (var i = 0; i < oldTriangles.Count; i += 3)
				{
					var a = oldTriangles[i + 0];
					var b = oldTriangles[i + 1];
					var c = oldTriangles[i + 2];

					InsertTriangle(a, b, c);
				}

				var mesh = new Mesh();

				mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
				mesh.SetVertices(newVertices);
				mesh.SetNormals(newNormals);
				mesh.SetTangents(newTangents);
				mesh.SetUVs(channel, newCoords);
				mesh.SetTriangles(newTriangles, 0);

				return mesh;
			}
			else
			{
				return Object.Instantiate(original);
			}
		}

		private static void InsertTriangle(int a, int b, int c)
		{
			var coordA = oldCoords[a];
			var coordB = oldCoords[b];
			var coordC = oldCoords[c];

			var minX = Mathf.Min(Mathf.Min(coordA.x, coordB.x), coordC.x);
			var minY = Mathf.Min(Mathf.Min(coordA.y, coordB.y), coordC.y);
			var maxX = Mathf.Max(Mathf.Max(coordA.x, coordB.x), coordC.x);
			var maxY = Mathf.Max(Mathf.Max(coordA.y, coordB.y), coordC.y);

			var eps = 1.0f / 16384.0f;
			var min = new Vector2Int(Mathf.FloorToInt(minX + eps), Mathf.FloorToInt(minY + eps));
			var max = new Vector2Int(Mathf.FloorToInt(maxX - eps), Mathf.FloorToInt(maxY - eps));

			// Limit the triangle to 3x3 clones
			if (max.x - min.x > 2) max.x = min.x + 2;
			if (max.y - min.y > 2) max.y = min.y + 2;

			for (var x = min.x; x <= max.x; x++)
			{
				for (var y = min.y; y <= max.y; y++)
				{
					var offset = new Vector2(-x, -y);
					var index  = newVertices.Count;

					newVertices.Add(oldVertices[a]);
					newVertices.Add(oldVertices[b]);
					newVertices.Add(oldVertices[c]);

					newNormals.Add(oldNormals[a]);
					newNormals.Add(oldNormals[b]);
					newNormals.Add(oldNormals[c]);

					newTangents.Add(oldTangents[a]);
					newTangents.Add(oldTangents[b]);
					newTangents.Add(oldTangents[c]);

					newCoords.Add(coordA + offset);
					newCoords.Add(coordB + offset);
					newCoords.Add(coordC + offset);

					newTriangles.Add(index + 0);
					newTriangles.Add(index + 1);
					newTriangles.Add(index + 2);
				}
			}
		}
	}
}