using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using CW.Common;

namespace PaintInEditor
{
	public partial class CwPaintInEditor
	{
		private Vector2 sceneScrollPosition;

		private Vector2Int resizeSize;

		private CwEditorPaintable expandedPaintable;

		private static List<CwEditorPaintable> paintables = new List<CwEditorPaintable>();

		private static HashSet<Renderer> roots = new HashSet<Renderer>();

		private static List<Material> tempMaterials = new List<Material>();

		public static void ApplyPaintables()
		{
			foreach (var paintable in paintables)
			{
				paintable.Apply();
			}
		}

		public static void RemovePaintables()
		{
			CwEditorPaintable.RemoveAll();
		}

		private static void RunRoots()
		{
			roots.Clear();

			foreach (var transform in Selection.transforms)
			{
				RunRoots(transform);
			}

			for (var i = paintables.Count - 1; i >= 0; i--)
			{
				var paintable = paintables[i];

				if (roots.Contains(paintable.Target) == false)
				{
					if (paintable.Target != null)
					{
						roots.Add(paintable.Target);
					}
					else
					{
						paintable.Clear();

						paintables.RemoveAt(i);

						ApplyAllPaintables();
					}
				}
			}
		}

		private static void RunRoots(Transform t)
		{
			var r = t.GetComponent<Renderer>();

			if (r != null && r is MeshRenderer || r is SkinnedMeshRenderer || r is SpriteRenderer)
			{
				roots.Add(r);
			}

			foreach (Transform child in t)
			{
				RunRoots(child);
			}
		}

		private void DrawObjectsTab()
		{
			RunRoots();

			if (roots.Count > 0)
			{
				sceneScrollPosition = GUILayout.BeginScrollView(sceneScrollPosition, GUILayout.ExpandHeight(true));
					foreach (var root in roots)
					{
						EditorGUILayout.BeginHorizontal();
							EditorGUI.BeginDisabledGroup(true);
								EditorGUILayout.ObjectField(GUIContent.none, root.gameObject, typeof(GameObject), true, GUILayout.MinWidth(10));
							EditorGUI.EndDisabledGroup();
							if (GUILayout.Button("Analyze", EditorStyles.miniButton, GUILayout.ExpandWidth(false)) == true)
							{
								if (TryGetPrepared(root) == true && preparedSource != null && preparedMesh != null)
								{
									CwEditorMeshAnalysis.OpenWith(preparedSource, preparedMesh);
								}
							}
						EditorGUILayout.EndHorizontal();
						EditorGUI.indentLevel++;
							root.GetSharedMaterials(tempMaterials);

							for (var materialIndex = 0; materialIndex < tempMaterials.Count; materialIndex++)
							{
								var material = tempMaterials[materialIndex];

								EditorGUILayout.BeginHorizontal();
									EditorGUI.BeginDisabledGroup(true);
										EditorGUILayout.ObjectField(GUIContent.none, material, typeof(Material), true, GUILayout.MinWidth(10));
									EditorGUI.EndDisabledGroup();
									if (paintables.Exists(p => p.Target == root) == false)
									{
										if (GUILayout.Button("Paint All", EditorStyles.miniButton, GUILayout.ExpandWidth(false)) == true)
										{
											PaintAll(root, materialIndex, material);

											if (Settings.AutoDeselectPaint == true)
											{
												TryDeselect(root);
											}
										}
									}
									else
									{
										CwEditor.BeginColor(Color.green, paintables.Exists(p => p.Target == root) == true);
											if (GUILayout.Button(new GUIContent("Export", "Export this material and all its textures to your project as assets?"), EditorStyles.miniButton, GUILayout.ExpandWidth(false)) == true)
											{
												TryExport(root, materialIndex, material);
											}
										CwEditor.EndColor();
									}
								EditorGUILayout.EndHorizontal();
								EditorGUI.indentLevel++;
									TryDrawMaterial(root, materialIndex, material);
								EditorGUI.indentLevel--;
							}
						EditorGUI.indentLevel--;
					}
					GUILayout.FlexibleSpace();
				GUILayout.EndScrollView();
			}
			else
			{
				GUILayout.Label("Select GameObjects with Renderers!", GetTitleBold(), GUILayout.ExpandWidth(true));

				GUILayout.FlexibleSpace();
			}

			DrawObjectsFooter();
		}

		private void DrawObjectsFooter()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
				EditorGUILayout.Separator();

				EditorGUI.BeginDisabledGroup(paintables.Exists(p => p.Asset != null && p.NeedsExporting == true) == false);
					CwEditor.BeginColor(Color.green);
						if (GUILayout.Button("Re-Export All", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)) == true)
						{
							if (EditorUtility.DisplayDialog("Are you sure?", "This will re-export all paintable textures in your scene.", "OK") == true)
							{
								foreach (var paintable in paintables)
								{
									if (paintable.Asset != null && paintable.NeedsExporting == true)
									{
										var finalPath = AssetDatabase.GetAssetPath(paintable.Asset);

										paintable.Asset = WriteTextureDataAndLoad(paintable, finalPath);
									}
								}

								AssetDatabase.Refresh();
							}
						}
					CwEditor.EndColor();
				EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
		}

		private void TryExport(Renderer renderer, int materialIndex, Material material)
		{
			var path = AssetDatabase.GetAssetPath(material);
			var dir  = string.IsNullOrEmpty(path) == false ? System.IO.Path.GetDirectoryName(path) : "Assets";

			path = EditorUtility.SaveFilePanelInProject("Export Material & Textures", name, "mat", "Export Your Material and Textures", dir);

			if (string.IsNullOrEmpty(path) == false)
			{
				var clone = Instantiate(material);

				AssetDatabase.CreateAsset(clone, path);

				foreach (var paintable in paintables)
				{
					if (paintable.Target == renderer && paintable.MaterialIndex == materialIndex)
					{
						var finalPath = System.IO.Path.GetDirectoryName(path) + "/" + System.IO.Path.GetFileNameWithoutExtension(path) + paintable.SlotName + "." + GetExtension(Settings.Format);

						paintable.Asset = WriteTextureDataAndLoad(paintable, finalPath);

						clone.SetTexture(paintable.SlotName, paintable.Asset);
					}
				}

				EditorUtility.SetDirty(this);
			}
		}

		private void PaintAll(Renderer renderer, int materialIndex, Material material)
		{
			if (material != null)
			{
				var shader = material.shader;

				if (shader != null)
				{
					var shaderPath = shader.name;
					var preset     = CwEditorShaderProfile_Editor.CachedInstances.Find(p => p.ShaderPaths.Contains(shaderPath));

					if (preset != null)
					{
						for (var i = 0; i < preset.Slots.Count; i++)
						{
							var slot = preset.Slots[i];

							MakePaintable(renderer, materialIndex, slot, false);
						}
					}
				}
			}
		}

		private void TryDrawMaterial(Renderer renderer, int materialIndex, Material material)
		{
			if (material != null)
			{
				var shader = material.shader;

				if (shader != null)
				{
					var shaderPath = shader.name;
					var preset     = CwEditorShaderProfile_Editor.CachedInstances.Find(p => p.ShaderPaths.Contains(shaderPath));

					if (preset != null)
					{
						for (var i = 0; i < preset.Slots.Count; i++)
						{
							var slot = preset.Slots[i];

							DrawSlot(renderer, materialIndex, slot);
						}
					}
					else
					{
						if (CwEditor.HelpButton("This material uses the '" + shader.name + "' shader, but there is no shader profile for it.", MessageType.Error, "Create", 60) == true)
						{
							CwEditorShaderProfile_Editor.CreateShaderProfileAsset(shader.name);
						}
					}
				}
			}
		}

		private void DrawSlot(Renderer target, int materialIndex, CwEditorShaderProfile.Slot slot)
		{
			var remove         = false;
			var paintableIndex = paintables.FindIndex(p => p.Target == target && p.MaterialIndex == materialIndex && p.SlotName == slot.Name && p.SlotName == slot.Name);

			if (paintableIndex != -1)
			{
				var paintable = paintables[paintableIndex];
				var expanded  = paintable == expandedPaintable;

				EditorGUILayout.BeginHorizontal();
					var newExpanded = EditorGUILayout.Foldout(expanded, slot.TextureProfileName, true);
					CwEditor.BeginColor(Color.red, paintable.Asset == null);
						EditorGUILayout.ObjectField(paintable.Asset, typeof(Texture2D), true, GUILayout.Width(150));
					CwEditor.EndColor();
					if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.ExpandWidth(false)) == true)
					{
						if (Settings.WarnRemoveTexture == false || EditorUtility.DisplayDialog("Remove?", "Are you sure you want to stop painting this texture?", "Yes", "No") == true)
						{
							remove = true;
						}
					}
				EditorGUILayout.EndHorizontal();

				if (expanded == false && newExpanded == true)
				{
					expandedPaintable = paintable;
				}

				if (expanded == true && newExpanded == false)
				{
					expandedPaintable = null;
				}

				if (newExpanded == true)
				{
					expandedPaintable = paintable;

					EditorGUI.indentLevel++;
						if (paintable.Current != null)
						{
							resizeSize.x = EditorGUILayout.IntField("Width = " + paintable.Current.width, resizeSize.x);
							resizeSize.y = EditorGUILayout.IntField("Height = " + paintable.Current.width, resizeSize.y);
							paintable.TextureProfile = (CwEditorTextureProfile)EditorGUILayout.ObjectField("Texture Profile", paintable.TextureProfile, typeof(CwEditorTextureProfile), true);
							paintable.Original = (Texture)EditorGUILayout.ObjectField("Original", paintable.Original, typeof(Texture), true, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.ExpandWidth(true));
							EditorGUI.BeginDisabledGroup(true);
								EditorGUILayout.ObjectField("Current", paintable.Current, typeof(Texture), true, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.ExpandWidth(true));
							EditorGUI.EndDisabledGroup();
							EditorGUILayout.BeginHorizontal();
								GUILayout.Space(EditorGUI.indentLevel * 15f);
								CwEditor.BeginColor(Color.green, paintable.Asset == null || paintable.NeedsExporting == true);
									if (GUILayout.Button(new GUIContent("Export", "Export this texture to the project as an asset?"), EditorStyles.miniButton) == true)
									{
										ExportTexture(paintable);
									}
								CwEditor.EndColor();
								EditorGUI.BeginDisabledGroup(paintable.CanResize(resizeSize) == false);
									if (GUILayout.Button(new GUIContent("Resize", "Export this texture to the project as an asset?"), EditorStyles.miniButton) == true)
									{
										var sizeA = paintable.Current.width + "x" + paintable.Current.height;
										var sizeB = resizeSize.x + "x" + resizeSize.y;

										if (Settings.WarnResizeTexture == false || EditorUtility.DisplayDialog("Resize?", "Are you sure you want to resize this texture from " + sizeA + " to " + sizeB + "?", "Yes", "No") == true)
										{
											paintable.Resize(resizeSize);
										}
									}
								EditorGUI.EndDisabledGroup();
								if (GUILayout.Button(new GUIContent("Erase", "Erase any paint applied to this texture?"), EditorStyles.miniButton) == true)
								{
									if (Settings.WarnEraseTexture == false || EditorUtility.DisplayDialog("Erase?", "Are you sure you want to erase all paint applied to this texture?", "Yes", "No") == true)
									{
										paintable.Erase();
									}
								}
							EditorGUILayout.EndHorizontal();
						}
					EditorGUI.indentLevel--;
				}

				if (remove == true)
				{
					paintable.Clear();

					paintables.RemoveAt(paintableIndex);

					ApplyAllPaintables();
				}
			}
			else
			{
				EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField(slot.TextureProfileName, GUILayout.MinWidth(10));
					if (GUILayout.Button("Paint", EditorStyles.miniButton, GUILayout.ExpandWidth(false)) == true)
					{
						MakePaintable(target, materialIndex, slot, true);
					}
				EditorGUILayout.EndHorizontal();
			}
		}

		private void ExportTexture(CwEditorPaintable paintable)
		{
			var path = AssetDatabase.GetAssetPath(paintable.Asset);
			var name = paintable.Target.name + "_" + paintable.SlotName;
			var dir  = string.IsNullOrEmpty(path) == false ? System.IO.Path.GetDirectoryName(path) : "Assets";

			if (string.IsNullOrEmpty(path) == false)
			{
				name = System.IO.Path.GetFileNameWithoutExtension(path);
			}

			path = EditorUtility.SaveFilePanelInProject("Export Texture", name, GetExtension(Settings.Format), "Export Your Texture", dir);

			if (string.IsNullOrEmpty(path) == false)
			{
				paintable.Asset = paintable.Asset = WriteTextureDataAndLoad(paintable, path);

				EditorUtility.SetDirty(this);
			}
		}

		private static void ApplyAllPaintables()
		{
			foreach (var paintable in paintables)
			{
				paintable.Apply();
			}
		}

		private bool MakePaintable(Renderer target, int materialIndex, CwEditorShaderProfile.Slot slot, bool warning)
		{
			var textureProfile = CwEditorTextureProfile_Editor.CachedInstances.Find(p => p.name == slot.TextureProfileName);

			if (textureProfile != null)
			{
				if (TryGetPrepared(target) == true)
				{
					var paintable = new CwEditorPaintable();

					paintable.Target         = target;
					paintable.TargetSource   = preparedSource;
					paintable.MaterialIndex  = materialIndex;
					paintable.TextureProfile = textureProfile;
					paintable.SlotName       = slot.Name;
					paintable.SlotKeywords   = slot.Keywords;
					paintable.SlotChannel    = (int)slot.Texcoord;
					paintable.paintMesh      = CwEditorMesh.GetProcessedCopy(preparedMesh, (int)slot.Texcoord);

					paintable.Initialize();

					paintables.Add(paintable);

					return true;
				}
				else
				{
					Debug.LogError("Failed to make this GameObject paintable.");
				}
			}
			else if (warning == true)
			{
				Debug.LogError("Failed to make this texture paintable because no texture profile exists for it.");
			}

			return false;
		}
	}
}