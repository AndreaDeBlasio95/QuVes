using System.Collections.Generic;
using UnityEngine;
using CW.Common;

namespace PaintInEditor
{
	public class CwEditorTextureProfile : ScriptableObject
	{
		public enum Channel
		{
			Unused = -1,
			X = 0,
			Y = 1,
			Z = 2,
			W = 3
		}

		public enum ExportType
		{
			Linear,
			sRGB,
			Normal
		}

		[System.Serializable]
		public struct Subset
		{
			public string  Name;
			public Channel R;
			public Channel G;
			public Channel B;
			public Channel A;

			public Vector4 SwizzleR
			{
				get
				{
					return SetIndexMask(R);
				}
			}

			public Vector4 SwizzleG
			{
				get
				{
					return SetIndexMask(G);
				}
			}

			public Vector4 SwizzleB
			{
				get
				{
					return SetIndexMask(B);
				}
			}

			public Vector4 SwizzleA
			{
				get
				{
					return SetIndexMask(A);
				}
			}

			public Vector4 SwizzleX
			{
				get
				{
					return FindIndexMask(Channel.X);
				}
			}

			public Vector4 SwizzleY
			{
				get
				{
					return FindIndexMask(Channel.Y);
				}
			}

			public Vector4 SwizzleZ
			{
				get
				{
					return FindIndexMask(Channel.Z);
				}
			}

			public Vector4 SwizzleW
			{
				get
				{
					return FindIndexMask(Channel.W);
				}
			}

			private static Vector4 SetIndexMask(Channel c)
			{
				var s = Vector4.zero;
				if (c != Channel.Unused)
				{
					s[(int)c] = 1.0f;
				}
				return s;
			}

			private Vector4 FindIndexMask(Channel c)
			{
				var s = Vector4.zero;
				     if (R == c) s[0] = 1.0f;
				else if (G == c) s[1] = 1.0f;
				else if (B == c) s[2] = 1.0f;
				else if (A == c) s[3] = 1.0f;
				return s;
			}
		}

		public Texture DefaultTexture { set { defaultTexture = value; } get { return defaultTexture; } } [SerializeField] private Texture defaultTexture;

		public ExportType Export { set { export = value; } get { return export; } } [SerializeField] private ExportType export;

		public Subset[] Subsets { set { subsets = value; } get { return subsets; } } [SerializeField] private Subset[] subsets;

		public bool IsNormalMap
		{
			get
			{
				if (subsets != null && subsets.Length == 1 && subsets[0].Name == "Normal")
				{
					return true;
				}

				return false;
			}
		}
	}
}

#if UNITY_EDITOR
namespace PaintInEditor
{
	using UnityEditor;
	using TARGET = CwEditorTextureProfile;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class CwEditorTextureProfile_Editor : CwEditor
	{
		private static List<CwEditorTextureProfile> cachedInstances;

		public static List<CwEditorTextureProfile> CachedInstances
		{
			get
			{
				if (cachedInstances == null)
				{
					cachedInstances = new List<CwEditorTextureProfile>();

					foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:CwEditorTextureProfile"))
					{
						var instance = UnityEditor.AssetDatabase.LoadAssetAtPath<CwEditorTextureProfile>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));

						cachedInstances.Add(instance);
					}
				}

				return cachedInstances;
			}
		}

		public static void ClearCache()
		{
			cachedInstances = null;
		}

		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			Draw("defaultTexture");
			Draw("export");
			Draw("subsets");
		}

		[MenuItem("Assets/Create/CW/Paint in Editor/Texture Profile")]
		private static void CreateAsset()
		{
			var asset = CreateInstance<CwEditorTextureProfile>();
			var guids = Selection.assetGUIDs;
			var path  = guids.Length > 0 ? AssetDatabase.GUIDToAssetPath(guids[0]) : null;

			if (string.IsNullOrEmpty(path) == true)
			{
				path = "Assets";
			}
			else if (AssetDatabase.IsValidFolder(path) == false)
			{
				path = System.IO.Path.GetDirectoryName(path);
			}

			var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + typeof(CwEditorTextureProfile).ToString() + ".asset");

			AssetDatabase.CreateAsset(asset, assetPathAndName);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			EditorUtility.FocusProjectWindow();

			Selection.activeObject = asset; EditorGUIUtility.PingObject(asset);

			cachedInstances.Add(asset);
		}
	}
}
#endif