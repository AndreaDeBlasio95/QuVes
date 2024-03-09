using UnityEngine;
using System.Collections.Generic;
using CW.Common;
using UnityEditor;

namespace PaintInEditor
{
	/// <summary>This tool can generate UV maps for a mesh that either doesn't have any, or has unsuitable UVs.</summary>
	[ExecuteInEditMode]
	[HelpURL(CwCommon.HelpUrlPrefix + "CwMeshUnwrapper")]
	public class CwMeshUnwrapper : ScriptableObject
	{
		[System.Serializable]
		public class Pair
		{
			/// <summary>The original mesh.</summary>
			public Mesh Source;

			/// <summary>The unwrapped mesh.</summary>
			public Mesh Output;
		}

		/// <summary>The meshes we will unwrap.</summary>
		public List<Pair> Meshes { get { if (meshes == null) meshes = new List<Pair>(); return meshes; } } [SerializeField] private List<Pair> meshes;

		/// <summary>This allows you to add a mesh to the mesh unwrapper.
		/// NOTE: You must later call <b>Generate</b> to unwrap the added meshes.</summary>
		public void AddMesh(Mesh mesh)
		{
			if (mesh != null)
			{
				Meshes.Add(new Pair() { Source = mesh });
			}
		}

		[ContextMenu("Generate")]
		public void Generate()
		{
#if UNITY_EDITOR
			UnityEditor.Undo.RecordObject(this, "Generate Unwrapped Meshes");
#endif

			if (meshes != null)
			{
				foreach (var pair in meshes)
				{
					if (pair.Source != null)
					{
						if (pair.Output == null)
						{
							pair.Output = new Mesh();
						}

						pair.Output.name = pair.Source.name + " (Unwrapped)";

						Generate(pair.Source, pair.Output);
					}
					else
					{
						DestroyImmediate(pair.Output);

						pair.Output = null;
					}
				}
			}
#if UNITY_EDITOR
			if (CwHelper.IsAsset(this) == true)
			{
				var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(UnityEditor.AssetDatabase.GetAssetPath(this));

				for (var i = 0; i < assets.Length; i++)
				{
					var assetMesh = assets[i] as Mesh;

					if (assetMesh != null)
					{
						if (meshes == null || meshes.Exists(p => p.Output == assetMesh) == false)
						{
							DestroyImmediate(assetMesh, true);
						}
					}
				}

				if (meshes != null)
				{
					foreach (var pair in meshes)
					{
						if (pair.Output != null && CwHelper.IsAsset(pair.Output) == false)
						{
							UnityEditor.AssetDatabase.AddObjectToAsset(pair.Output, this);

							UnityEditor.AssetDatabase.SaveAssets();
						}
					}
				}
			}

			if (CwHelper.IsAsset(this) == true)
			{
				CwHelper.ReimportAsset(this);
			}

			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}

		/// <summary>This static method allows you to unwrap the source mesh at runtime.</summary>
		public static void Generate(Mesh source, Mesh output)
		{
			if (source != null && output != null)
			{
				// Copy mesh data
				var submeshes = new List<List<int>>();
				var indices   = new List<int>();

				for (var i = 0; i < source.subMeshCount; i++)
				{
					source.GetTriangles(indices, i);

					submeshes.Add(indices);
				}

				CopyMesh(source, output, submeshes);

				Unwrapping.GenerateSecondaryUVSet(output);

				output.uv = output.uv2;
			}
		}

		private static void CopyMesh(Mesh source, Mesh output, List<List<int>> submeshes)
		{
			output.bindposes    = source.bindposes;
			output.bounds       = source.bounds;
			output.subMeshCount = source.subMeshCount;
			output.indexFormat  = source.indexFormat;

			var weights   = new List<BoneWeight>(); source.GetBoneWeights(weights);
			var colors    = new List<Color32>();    source.GetColors(colors);
			var normals   = new List<Vector3>();    source.GetNormals(normals);
			var tangents  = new List<Vector4>();    source.GetTangents(tangents);
			var coords0   = new List<Vector4>();    source.GetUVs(0, coords0);
			var coords1   = new List<Vector4>();    source.GetUVs(1, coords1);
			var coords2   = new List<Vector4>();    source.GetUVs(2, coords2);
			var coords3   = new List<Vector4>();    source.GetUVs(3, coords3);
			var positions = new List<Vector3>();    source.GetVertices(positions);
			var boneVertices = new List<byte>(source.GetBonesPerVertex());
			var boneWeights  = new List<BoneWeight1>(source.GetAllBoneWeights());
			var boneIndices  = new List<int>();

			if (boneVertices.Count > 0)
			{
				var total = 0;

				foreach (var count in boneVertices)
				{
					boneIndices.Add(total);
				
					total += count;
				}

				weights.Clear();
			}

			output.SetVertices(positions);

			if (weights.Count > 0)
			{
				output.boneWeights = weights.ToArray();
			}

			if (boneVertices.Count > 0)
			{
				var na1 = new Unity.Collections.NativeArray<byte>(boneVertices.ToArray(), Unity.Collections.Allocator.Temp);
				var na2 = new Unity.Collections.NativeArray<BoneWeight1>(boneWeights.ToArray(), Unity.Collections.Allocator.Temp);
				output.SetBoneWeights(na1, na2);
				na2.Dispose();
				na1.Dispose();
			}

			output.SetColors(colors);
			output.SetNormals(normals);
			output.SetTangents(tangents);
			output.SetUVs(0, coords0);
			output.SetUVs(1, coords1);
			output.SetUVs(2, coords2);
			output.SetUVs(3, coords3);

			var deltaVertices = new List<Vector3>();
			var deltaNormals = new List<Vector3>();
			var deltaTangents = new List<Vector3>();

			if (source.blendShapeCount > 0)
			{
				var tempDeltaVertices = new Vector3[source.vertexCount];
				var tempDeltaNormals  = new Vector3[source.vertexCount];
				var tempDeltaTangents = new Vector3[source.vertexCount];

				for (var i = 0; i < source.blendShapeCount; i++)
				{
					var shapeName  = source.GetBlendShapeName(i);
					var frameCount = source.GetBlendShapeFrameCount(i);

					for (var j = 0; j < frameCount; j++)
					{
						source.GetBlendShapeFrameVertices(i, j, tempDeltaVertices, tempDeltaNormals, tempDeltaTangents);

						deltaVertices.Clear();
						deltaNormals.Clear();
						deltaTangents.Clear();

						deltaVertices.AddRange(tempDeltaVertices);
						deltaNormals.AddRange(tempDeltaNormals);
						deltaTangents.AddRange(tempDeltaTangents);

						output.AddBlendShapeFrame(shapeName, source.GetBlendShapeFrameWeight(i, j), deltaVertices.ToArray(), deltaNormals.ToArray(), deltaTangents.ToArray());
					}
				}
			}

			for (var i = 0; i < submeshes.Count; i++)
			{
				output.SetTriangles(submeshes[i], i);
			}
		}
	}
}

#if UNITY_EDITOR
namespace PaintInEditor
{
	using UnityEditor;
	using TARGET = CwMeshUnwrapper;

	[CustomEditor(typeof(TARGET))]
	public class CwMeshUnwrapper_Editor : CwEditor
	{
		private Texture2D sourceTexture;

		private enum SquareSizes
		{
			Square64    =    64,
			Square128   =   128,
			Square256   =   256,
			Square512   =   512,
			Square1024  =  1024,
			Square2048  =  2048,
			Square4096  =  4096,
			Square8192  =  8192,
			Square16384 = 16384,
		}

		private SquareSizes newSize = SquareSizes.Square1024;

		private Dictionary<Mesh, Mesh> pairs = new Dictionary<Mesh, Mesh>();

		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			EditorGUILayout.HelpBox("This tool can generate UV maps for a mesh that either doesn't have any, or has unsuitable UVs.", MessageType.Info);

			Separator();

			var sMeshes = serializedObject.FindProperty("meshes");
			var sDel    = -1;
			var missing = false;

			EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Meshes");
				BeginColor(Color.green, Any(tgts, t => t.Meshes.Count == 0));
					if (GUILayout.Button("Add", EditorStyles.miniButton, GUILayout.ExpandWidth(false)) == true)
					{
						sMeshes.InsertArrayElementAtIndex(sMeshes.arraySize);
					}
				EndColor();
			EditorGUILayout.EndHorizontal();

			EditorGUI.indentLevel++;
				for (var i = 0; i < tgt.Meshes.Count; i++)
				{
					var sSource = sMeshes.GetArrayElementAtIndex(i).FindPropertyRelative("Source");

					EditorGUILayout.BeginHorizontal();
						BeginError(sSource.objectReferenceValue == null);
							EditorGUILayout.PropertyField(sSource, GUIContent.none);
						EndError();
						var sourceMesh = tgt.Meshes[i].Source;
						BeginDisabled(sourceMesh == null);
							if (GUILayout.Button("Analyze Old", EditorStyles.miniButton, GUILayout.ExpandWidth(false)) == true)
							{
								CwEditorMeshAnalysis.OpenWith(sourceMesh, sourceMesh);
							}
						EndDisabled();
						var outputMesh = tgt.Meshes[i].Output;
						if (outputMesh == null)
						{
							missing = true;
						}
						BeginDisabled(outputMesh == null);
							if (GUILayout.Button("Analyze New", EditorStyles.miniButton, GUILayout.ExpandWidth(false)) == true)
							{
								CwEditorMeshAnalysis.OpenWith(outputMesh, outputMesh);
							}
							//EditorGUILayout.ObjectField(GUIContent.none, outputMesh, typeof(Mesh), false, GUILayout.Width(80));
						EndDisabled();
						if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.ExpandWidth(false)) == true)
						{
							sDel = i;
						}
					EditorGUILayout.EndHorizontal();
				}
			EditorGUI.indentLevel--;

			if (sDel >= 0)
			{
				sMeshes.DeleteArrayElementAtIndex(sDel);
			}

			Separator();

			BeginColor(Color.green, missing);
				if (Button("Generate") == true)
				{
					Each(tgts, t => t.Generate());
				}
			EndColor();

			Separator();
			Separator();

			EditorGUILayout.LabelField("REMAP TEXTURE", EditorStyles.boldLabel);

			sourceTexture = (Texture2D)EditorGUILayout.ObjectField(sourceTexture, typeof(Texture2D), false);

			if (sourceTexture != null)
			{
				newSize = (SquareSizes)EditorGUILayout.EnumPopup("New Size", newSize);

				Separator();

				EditorGUILayout.LabelField("REMAP WITH MESH", EditorStyles.boldLabel);

				for (var i = 0; i < tgt.Meshes.Count; i++)
				{
					var pair = tgt.Meshes[i];

					if (pair != null && pair.Source != null && pair.Output != null)
					{
						if (GUILayout.Button(pair.Source.name) == true)
						{
							Remap(sourceTexture, pair.Source, pair.Output, (int)newSize);
						}
					}
				}
			}

			if (tgts.Length == 1)
			{
				Separator();
				Separator();

				EditorGUILayout.LabelField("SWAP MESHES", EditorStyles.boldLabel);

				pairs.Clear();

				foreach (var pair in tgt.Meshes)
				{
					if (pair.Source != null && pair.Output != null)
					{
						pairs.Add(pair.Source, pair.Output);
					}
				}

				Mesh output;

				var count = 0;

				foreach (var r in FindObjectsOfType<Renderer>())
				{
					var mf  = r.GetComponent<MeshFilter>();
					var smr = r.GetComponent<SkinnedMeshRenderer>();
					var m   = mf != null ? mf.sharedMesh : (smr != null ? smr.sharedMesh : null);

					if (m != null && pairs.TryGetValue(m, out output) == true)
					{
						EditorGUILayout.BeginHorizontal();
							BeginDisabled();
								EditorGUILayout.ObjectField(r.gameObject, typeof(GameObject), true);
							EndDisabled();
							if (GUILayout.Button("Swap", GUILayout.ExpandWidth(false)) == true)
							{
								if (mf != null)
								{
									Undo.RecordObject(mf, "Swap Mesh"); mf.sharedMesh = output; EditorUtility.SetDirty(mf);
								}
								else if (smr != null)
								{
									Undo.RecordObject(smr, "Swap Mesh"); smr.sharedMesh = output; EditorUtility.SetDirty(smr);
								}
							}
						EditorGUILayout.EndHorizontal();

						count += 1;
					}
				}

				if (count == 0)
				{
					Info("If your scene contains any MeshFilter/SkinnedMeshRenderer components using the original non-fixed mesh, then they will be listed here.");
				}
			}
		}

		private static void Remap(Texture2D sourceTexture, Mesh oldMesh, Mesh newMesh, int newSize)
		{
			var path = AssetDatabase.GetAssetPath(sourceTexture);
			var name = sourceTexture.name;
			var dir  = string.IsNullOrEmpty(path) == false ? System.IO.Path.GetDirectoryName(path) : "Assets";

			if (string.IsNullOrEmpty(path) == false)
			{
				name = System.IO.Path.GetFileNameWithoutExtension(path);
			}

			name += " (Remapped)";

			path = EditorUtility.SaveFilePanelInProject("Export Texture", name, "png", "Export Your Texture", dir);

			if (string.IsNullOrEmpty(path) == false)
			{
				var remapTexture = CwEditorRemap.Remap(sourceTexture, oldMesh, newMesh, newSize);

				CwEditorRemap.Export(remapTexture, path, sourceTexture);

				DestroyImmediate(remapTexture);
			}
		}

		[MenuItem("CONTEXT/Mesh/Mesh Unwrapper (Paint in Editor)")]
		[MenuItem("CONTEXT/ModelImporter/Mesh Unwrapper (Paint in Editor)")]
		public static void Create(MenuCommand menuCommand)
		{
			var sources = new List<Mesh>();
			var mesh    = menuCommand.context as Mesh;
			var name    = "";

			if (mesh != null)
			{
				sources.Add(mesh);

				name = mesh.name;
			}
			else
			{
				var modelImporter = menuCommand.context as ModelImporter;

				if (modelImporter != null)
				{
					var assets = AssetDatabase.LoadAllAssetsAtPath(modelImporter.assetPath);

					for (var i = 0; i < assets.Length; i++)
					{
						var assetMesh = assets[i] as Mesh;

						if (assetMesh != null)
						{
							sources.Add(assetMesh);
						}
					}

					name = System.IO.Path.GetFileNameWithoutExtension(modelImporter.assetPath);
				}
			}
			
			if (sources.Count > 0)
			{
				var path = AssetDatabase.GetAssetPath(menuCommand.context);

				if (string.IsNullOrEmpty(path) == false)
				{
					path = System.IO.Path.GetDirectoryName(path);
				}
				else
				{
					path = "Assets";
				}

				path += "/Mesh Unwrapper (" + name + ").asset";

				var instance = CreateInstance<CwMeshUnwrapper>();

				foreach (var source in sources)
				{
					instance.AddMesh(source);
				}

				ProjectWindowUtil.CreateAsset(instance, path);
			}
		}

		[MenuItem("Assets/Create/CW/Paint in Editor/Mesh Unwrapper")]
		private static void CreateAsset()
		{
			var guids = Selection.assetGUIDs;

			CreateMeshUnwrapperAsset(default(Mesh), guids.Length > 0 ? AssetDatabase.GUIDToAssetPath(guids[0]) : default(string));
		}

		public static void CreateMeshUnwrapperAsset(Mesh mesh)
		{
			CreateMeshUnwrapperAsset(mesh, AssetDatabase.GetAssetPath(mesh));
		}

		public static void CreateMeshUnwrapperAsset(Mesh mesh, string path)
		{
			var asset = CreateInstance<CwMeshUnwrapper>();
			var name  = "Mesh Unwrapper";

			if (string.IsNullOrEmpty(path) == true || path.StartsWith("Library/", System.StringComparison.InvariantCultureIgnoreCase))
			{
				path = "Assets";
			}
			else if (AssetDatabase.IsValidFolder(path) == false)
			{
				path = System.IO.Path.GetDirectoryName(path);
			}

			if (mesh != null)
			{
				var meshPath      = AssetDatabase.GetAssetPath(mesh);
				var modelImporter = AssetImporter.GetAtPath(meshPath);

				if (modelImporter != null)
				{
					foreach (var o in AssetDatabase.LoadAllAssetsAtPath(modelImporter.assetPath))
					{
						var assetMesh = o as Mesh;

						if (assetMesh is Mesh)
						{
							asset.AddMesh(assetMesh);
						}
					}

					name += " (" + System.IO.Path.GetFileNameWithoutExtension(modelImporter.assetPath) + ")";
				}
				else
				{
					name += " (" + mesh.name + ")";

					asset.AddMesh(mesh);
				}
			}

			var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + name + ".asset");

			AssetDatabase.CreateAsset(asset, assetPathAndName);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			EditorUtility.FocusProjectWindow();

			Selection.activeObject = asset; EditorGUIUtility.PingObject(asset);
		}
	}
}
#endif