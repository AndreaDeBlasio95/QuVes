using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PaintInEditor
{
	public abstract class CwEditorBrush : ScriptableObject, CwPaintInEditor.IBrowsable
	{
		public enum BlendType
		{
			Replace,
			Combine,
			Erase
		}

		public Texture2D Icon { set { icon = value; } get { return icon; } } [SerializeField] protected Texture2D icon;

		public BlendType Blend { set { blend = value; } get { return blend; } } [SerializeField] protected BlendType blend;

		public abstract bool NeedsShape
		{
			get;
		}

		public string GetTitle()
		{
			return name;
		}

		public Texture GetIcon()
		{
			if (icon == null)
			{
			}

			return icon;
		}

		public Object GetObject()
		{
			return this;
		}

		public abstract Texture GetShape();

		public abstract float GetSize();

		public abstract float GetAngle();

		public abstract void DrawSettings(CwPaintInEditor.SettingsData settings);

		public abstract void DrawBrush(CwPaintInEditor.SettingsData settings, CwEditorPaintable paintable, CwEditorTextureProfile.Subset subset, double oldDistance, Vector2 oldPoint, Vector2 newPoint, bool down);
	}
}

#if UNITY_EDITOR
namespace PaintInEditor
{
	using CW.Common;
	using UnityEditor;
	using TARGET = CwEditorBrush;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class CwEditorBrush_Editor : CwEditor
	{
		private static List<CwEditorBrush> cachedInstances;
		public static List<CwEditorBrush> CachedInstances
		{
			get
			{
				if (cachedInstances == null)
				{
					cachedInstances = new List<CwEditorBrush>();

					foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:CwEditorBrush"))
					{
						var instance = UnityEditor.AssetDatabase.LoadAssetAtPath<CwEditorBrush>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));

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

		protected static List<Texture2D> tempTextures = new List<Texture2D>();

		private static void BuildTempTextures()
		{
			tempTextures.Clear();

			foreach (var o in Selection.objects)
			{
				if (o is Texture2D)
				{
					tempTextures.Add((Texture2D)o);
				}
			}
		}

		protected static T CreateAsset_AndBuildTempTextures<T>(System.Action<T> populate)
			where T : CwEditorBrush
		{
			var asset = CreateInstance<T>();
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

			BuildTempTextures();

			var title = typeof(T).ToString();

			foreach (var t in tempTextures)
			{
				if (t.name.EndsWith("_Albedo", System.StringComparison.InvariantCultureIgnoreCase) ||
					t.name.EndsWith("_Normal", System.StringComparison.InvariantCultureIgnoreCase) ||
					t.name.EndsWith("_Metallic", System.StringComparison.InvariantCultureIgnoreCase) ||
					t.name.EndsWith("_Smoothness", System.StringComparison.InvariantCultureIgnoreCase))
				{
					title = t.name.Remove(t.name.LastIndexOf("_")); break;
				}
			}

			foreach (var t in tempTextures)
			{
				if (t.name == title)
				{
					asset.Icon = t; break;
				}
			}

			if (populate != null)
			{
				populate(asset);
			}

			var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + title + ".asset");

			AssetDatabase.CreateAsset(asset, assetPathAndName);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			EditorUtility.FocusProjectWindow();

			Selection.activeObject = asset; EditorGUIUtility.PingObject(asset);

			if (cachedInstances != null)
			{
				cachedInstances.Add(asset);
			}

			return asset;
		}

		protected void SaveIcon(CwEditorBrush tgt, RenderTexture thumb)
		{
			var path = AssetDatabase.GetAssetPath(tgt);

			if (tgt != null && thumb != null && string.IsNullOrEmpty(path) == false)
			{
				var texture = new Texture2D(thumb.width, thumb.height);

				CwHelper.BeginActive(thumb);
					texture.ReadPixels(new Rect(0, 0, thumb.width, thumb.height), 0, 0); texture.Apply();
				CwHelper.EndActive();

				var data = texture.EncodeToPNG();

				path = System.IO.Path.ChangeExtension(path, "png");

				System.IO.File.WriteAllBytes(path, data);

				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

				DestroyImmediate(texture);

				tgt.Icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

				EditorUtility.SetDirty(tgt); serializedObject.Update();
			}
		}

		protected void TryDrawWarnings(Texture t, bool normal, bool uncompressed, bool srgb)
		{
			if (t != null)
			{
				var path = AssetDatabase.GetAssetPath(t);

				if (string.IsNullOrEmpty(path) == false)
				{
					var importer = AssetImporter.GetAtPath(path) as TextureImporter;

					if (importer != null)
					{
						var warnings = false;

						if (normal == true && importer.textureType != TextureImporterType.NormalMap)
						{
							Warning("This this texture's Texture Type should be set to: Normal map"); warnings = true;
						}

						if (uncompressed == true && importer.textureCompression != TextureImporterCompression.Uncompressed)
						{
							Warning("This this texture's Compression should be set to: None"); warnings = true;
						}

						if (importer.sRGBTexture != srgb)
						{
							Warning("This this texture's sRGB should be set to: Off"); warnings = true;
						}

						if (warnings == true)
						{
							if (Button("Fix Issues") == true)
							{
								if (normal == true)
								{
									importer.textureType = TextureImporterType.NormalMap;
								}

								if (uncompressed == true)
								{
									importer.textureCompression = TextureImporterCompression.Uncompressed;
								}

								importer.sRGBTexture = srgb;

								importer.SaveAndReimport();
							}
						}
					}
				}
			}
		}
	}
}
#endif