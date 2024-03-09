using System.Collections.Generic;
using UnityEngine;
using CW.Common;

namespace PaintInEditor
{
	/// <summary>This object allows you to define a texture that can be used as a shape for in-editor painting.</summary>
	public class CwEditorShape : ScriptableObject, CwPaintInEditor.IBrowsable
	{
		/// <summary>The <b>Texture</b> this shape will use.
		/// NOTE: If this texture has an alpha channel, then the <b>Alpha</b> channel of the texture will be used for the shape. If not, the <b>Red</b> channel will be used.</summary>
		public Texture Texture { set { texture = value; } get { return texture; } } [SerializeField] private Texture texture;

		public string GetTitle()
		{
			return name;
		}

		public Texture GetIcon()
		{
			return texture;
		}

		public Object GetObject()
		{
			return this;
		}
	}
}

#if UNITY_EDITOR
namespace PaintInEditor
{
	using System.Linq;
	using UnityEditor;
	using TARGET = CwEditorShape;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class CwEditorShape_Editor : CwEditor
	{
		private static List<CwEditorShape> cachedInstances;

		public static List<CwEditorShape> CachedInstances
		{
			get
			{
				if (cachedInstances == null)
				{
					cachedInstances = new List<CwEditorShape>();

					foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:CwEditorShape"))
					{
						var instance = UnityEditor.AssetDatabase.LoadAssetAtPath<CwEditorShape>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));

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

			Draw("texture");
		}

		[MenuItem("Assets/Create/CW/Paint in Editor/Editor Shape")]
		private static void CreateAsset()
		{
			var selectedTextures = Selection.objects.Where(o => o is Texture).Cast<Texture>();

			if (selectedTextures.Count() > 0)
			{
				foreach (var t in selectedTextures)
				{
					CreateAsset(t, t.name);
				}
			}
			else
			{
				CreateAsset(null, typeof(CwEditorShape).ToString());
			}
		}

		private static void CreateAsset(Texture texture, string title)
		{
			var asset = CreateInstance<CwEditorShape>();
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

			var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + title + ".asset");

			asset.Texture = texture;

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