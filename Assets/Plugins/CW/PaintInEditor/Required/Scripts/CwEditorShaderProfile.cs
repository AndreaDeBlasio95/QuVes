using System.Collections.Generic;
using UnityEngine;
using CW.Common;

namespace PaintInEditor
{
	/// <summary>This object allows you to define what kind of paintable textures are in the specified shaders.</summary>
	public class CwEditorShaderProfile : ScriptableObject
	{
		public enum Texcoord
		{
			First,
			Second,
			Third,
			Fourth
		}

		[System.Serializable]
		public struct Slot
		{
			public string Name;

			[CwEditorTextureProfile]
			public string TextureProfileName;

			public Texcoord Texcoord;

			public string Keywords;
		}

		/// <summary>This shaer profile applies to these shaders.
		/// NOTE: Each shader in this list should have the same paintable textures.</summary>
		public List<string> ShaderPaths { get { if (shaderPaths == null) shaderPaths = new List<string>(); return shaderPaths; } } [SerializeField] private List<string> shaderPaths;

		/// <summary>The paintable textures that are in the specified shaders.</summary>
		public List<Slot> Slots { get { if (slots == null) slots = new List<Slot>(); return slots; } } [SerializeField] private List<Slot> slots = new List<Slot>();

		public bool ShaderHasSlots(Shader s)
		{
			if (slots != null)
			{
				foreach (var slot in slots)
				{
					if (ShaderHasTexture(s, slot.Name) == false)
					{
						return false;
					}
				}
			}
			

			return true;
		}

		private static bool ShaderHasTexture(Shader s, string t)
		{
			for (var i = 0; i < s.GetPropertyCount(); i++)
			{
				if (s.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture)
				{
					if (s.GetPropertyName(i) == t)
					{
						return true;
					}
				}
			}

			return false;
		}
	}
}

#if UNITY_EDITOR
namespace PaintInEditor
{
	using UnityEditor;
	using TARGET = CwEditorShaderProfile;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class CwEditorShaderProfile_Editor : CwEditor
	{
		private static List<CwEditorShaderProfile> cachedInstances;

		public static List<CwEditorShaderProfile> CachedInstances
		{
			get
			{
				if (cachedInstances == null)
				{
					cachedInstances = new List<CwEditorShaderProfile>();

					foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:CwEditorShaderProfile"))
					{
						var instance = UnityEditor.AssetDatabase.LoadAssetAtPath<CwEditorShaderProfile>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));

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

			if (tgts.Length == 1 && tgt.ShaderPaths.Count > 0)
			{
				var shader = Shader.Find(tgt.ShaderPaths[0]);

				if (shader != null)
				{
					Separator();

					EditorGUILayout.LabelField("Textures in Shader", EditorStyles.boldLabel);

					var count = ShaderUtil.GetPropertyCount(shader);

					for (var i = 0; i < count; i++)
					{
						if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
						{
							var slot = ShaderUtil.GetPropertyName(shader, i);
							var desc = ShaderUtil.GetPropertyDescription(shader, i);

							EditorGUILayout.BeginHorizontal();
								EditorGUILayout.LabelField(slot, desc);
								BeginDisabled(tgt.Slots.Exists(s => s.Name == slot));
									if (GUILayout.Button("+", GUILayout.ExpandWidth(false)) == true)
									{
										var menu = new GenericMenu();

										foreach (var p in CwEditorTextureProfile_Editor.CachedInstances)
										{
											if (p != null)
											{
												menu.AddItem(new GUIContent(p.name), false, () =>
												{
													Each(tgts, t => t.Slots.Add(new TARGET.Slot() { Name = slot, TextureProfileName = p.name }), true, true); serializedObject.Update();
												});
											}
										}
										menu.ShowAsContext();
									}
								EndDisabled();
							EditorGUILayout.EndHorizontal();
						}
					}

					EndDisabled();
				}
			}

			Separator();
			Separator();

			if (tgts.Length == 1 && tgt.Slots.Count > 0)
			{
				if (GUILayout.Button(new GUIContent("Add Matching Shader", "This will show you a list of all shaders in your project that have texture names that match the texture slots you've defined in this shader profile. If these other shaders are very similar, then you can add them to the same profile.")) == true)
				{
					var menu = new GenericMenu();

					foreach (var shaderInfo in ShaderUtil.GetAllShaderInfo())
					{
						if (tgt.ShaderPaths.Contains(shaderInfo.name) == false && shaderInfo.name.StartsWith("Hidden/") == false)
						{
							var shader = Shader.Find(shaderInfo.name);

							if (shader != null && tgt.ShaderHasSlots(shader) == true)
							{
								menu.AddItem(new GUIContent(shaderInfo.name), false, () => Each(tgts, t => { t.ShaderPaths.Add(shaderInfo.name); DirtyAndUpdate(); }, true, true));
							}
						}
					}

					menu.ShowAsContext();
				}
			}

			Separator();
			Separator();

			BeginError(Any(tgts, t => t.ShaderPaths.Count == 0));
				Draw("shaderPaths", "This shaer profile applies to these shaders.\n\nNOTE: Each shader in this list should have the same paintable textures.");
			EndError();

			BeginError(Any(tgts, t => t.Slots.Count == 0));
				Draw("slots", "The paintable textures that are in the specified shaders.");
			EndError();
		}

		[MenuItem("Assets/Create/CW/Paint in Editor/Shader Profile")]
		private static void CreateAsset()
		{
			CreateShaderProfileAsset();
		}

		public static CwEditorShaderProfile CreateShaderProfileAsset(string shaderPath = null)
		{
			var asset = CreateInstance<CwEditorShaderProfile>();
			var guids = Selection.assetGUIDs;
			var path  = guids.Length > 0 ? AssetDatabase.GUIDToAssetPath(guids[0]) : null;
			var name  = "Shader Profile";

			if (string.IsNullOrEmpty(shaderPath) == false)
			{
				asset.ShaderPaths.Add(shaderPath);

				foreach (var c in "\\/?<>:*|\"")
				{
					shaderPath = shaderPath.Replace(c, '_');
				}

				name += " (" + shaderPath + ")";
			}

			if (string.IsNullOrEmpty(path) == true)
			{
				path = "Assets";
			}
			else if (AssetDatabase.IsValidFolder(path) == false)
			{
				path = System.IO.Path.GetDirectoryName(path);
			}

			var assetPathAndName = AssetDatabase.GenerateUniqueAssetPath(path + "/" + name + ".asset");

			AssetDatabase.CreateAsset(asset, assetPathAndName);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			EditorUtility.FocusProjectWindow();

			Selection.activeObject = asset; EditorGUIUtility.PingObject(asset);

			ClearCache();

			return asset;
		}
	}
}
#endif