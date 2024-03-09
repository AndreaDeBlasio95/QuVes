using UnityEngine;
using UnityEditor;
using CW.Common;
using System.Reflection;
using System.Collections.Generic;

namespace PaintInEditor
{
	/// <summary>This is the main Paint in Editor window. This can be opened from <b>Window/CW/Paint in Editor</b> or from the <b>Scene</b> tab's tool bar by opening the custom tools dropdown and selecting <b>Paint in Editor</b>.</summary>
	public partial class CwPaintInEditor : EditorWindow
	{
		public enum PageType
		{
			Objects,
			Paint,
			Config
		}

		public static PageType CurrentPage;

		public static CwPaintInEditor Instance;

		private static Material outlineMaterial;

		private static Material depthMaterial;

		private static MethodInfo method_IntersectRayMesh;

		public static Material OutlineMaterial
		{
			get
			{
                if (outlineMaterial == null)
                {
                    outlineMaterial = CwHelper.CreateTempMaterial("Outline", "Hidden/PaintInEditor/CwOutline");
                }

                return outlineMaterial;
			}
		}

		public static Material DepthMaterial
		{
			get
			{
                if (depthMaterial == null)
                {
                    depthMaterial = CwHelper.CreateTempMaterial("Depth", "Hidden/PaintInEditor/CwDepth");
                }

                return depthMaterial;
			}
		}

		public static bool CanUndo
		{
			get
			{
				return paintables.Exists(p => p.UndoStates.Count > 0);
			}
		}

		public static bool CanRedo
		{
			get
			{
				return paintables.Exists(p => p.RedoStates.Count > 0);
			}
		}

		public static bool HasPaintables
		{
			get
			{
				foreach (var p in paintables)
				{
					if (p.Target != null)
					{
						return true;
					}
				}

				return false;
			}
		}

		static CwPaintInEditor() 
		{
			method_IntersectRayMesh = typeof(UnityEditor.HandleUtility).GetMethod("IntersectRayMesh", BindingFlags.Static | BindingFlags.NonPublic);
		}

		[MenuItem("Window/CW/Paint in Editor")]
		public static void OpenWindow()
		{
			GetWindow();
		}

		public static CwPaintInEditor GetWindow()
		{
			return GetWindow<CwPaintInEditor>("Paint In Editor", true);
		}

		public static void TryRepaint()
		{
			if (Instance != null)
			{
				Instance.Repaint();
			}
		}

		public static void StoreUndoState()
		{
			if (preparedState == false)
			{
				preparedState = true;

				paintables.ForEach(p => p.AddUndoState());
			}
		}

		public void AutoLockSelection()
		{
			RunRoots();

			foreach (var root in roots)
			{
				root.GetSharedMaterials(tempMaterials);

				for (var materialIndex = 0; materialIndex < tempMaterials.Count; materialIndex++)
				{
					var material = tempMaterials[materialIndex];

					if (material != null)
					{
						var shader = material.shader;

						if (shader != null)
						{
							var shaderPath = shader.name;
							var preset     = CwEditorShaderProfile_Editor.CachedInstances.Find(p => p.ShaderPaths.Contains(shaderPath));

							if (preset != null)
							{
								var found = false;

								for (var i = 0; i < preset.Slots.Count; i++)
								{
									var slot = preset.Slots[i];

									if (MakePaintable(root, materialIndex, slot, false) == true)
									{
										found = true;
									}
								}

								if (found == true)
								{
									TryDeselect(root);
								}
							}
						}
					}
				}
			}
		}

		private static List<Object> tempObjects = new List<Object>();

		private void TryDeselect(Renderer root)
		{
			tempObjects.Clear();

			tempObjects.AddRange(Selection.objects);

			for (var i = tempObjects.Count - 1; i >= 0; i--)
			{
				var tempObject = tempObjects[i];

				if (tempObject == root || tempObject == root.gameObject || tempObject == root.transform)
				{
					tempObjects.RemoveAt(i);
				}
			}

			Selection.objects = tempObjects.ToArray();
		}

		public void UndoAll()
		{
			foreach (var p in paintables)
			{
				p.Undo();
			}
		}

		public void RedoAll()
		{
			foreach (var p in paintables)
			{
				p.Redo();
			}
		}

		public static bool GetCursorShape(ref Texture texture, ref Vector2 size, ref float angle)
		{
			if (Settings.Brush != null)
			{
				var shape = Settings.Brush.GetShape();

				if (shape != null)
				{
					texture = shape;
					size    = new Vector2(Settings.Brush.GetSize(), Settings.Brush.GetSize());
					angle   = Settings.Brush.GetAngle();

					return true;
				}
			}

			return false;
		}

		protected virtual void OnEnable()
		{
			Instance = this;

			CwEditorTool.OnToolUpdate += HandleToolUpdate;

			LoadSettings();

			if (Settings.Brush == null)
			{
				var guids = AssetDatabase.FindAssets("t:CwEditorBrush_Wallpaper Brick_Wall_017");

				if (guids.Length > 0)
				{
					Settings.Brush = CwHelper.LoadAssetAtGUID<CwEditorBrush_Wallpaper>(guids[0]);
				}
			}

			if (Settings.Wallpaper.Shape == null)
			{
				var guids = AssetDatabase.FindAssets("t:CwEditorShape Circle F");

				if (guids.Length > 0)
				{
					Settings.Wallpaper.Shape = CwHelper.LoadAssetAtGUID<CwEditorShape>(guids[0]);
				}
			}
		}

		protected virtual void OnDisable()
		{
			CwEditorTool.OnToolUpdate -= HandleToolUpdate;

			SaveSettings();

			CwEditorTool.DeselectThisTool();

			if (paintables.Exists(p => p.Asset == null || p.NeedsExporting == true) == true)
			{
				if (EditorUtility.DisplayDialog("Export Unsaved Textures?", "You have unsaved painted textures. Do you want to save them?", "Yes", "No") == true)
				{
					RunRoots();

					foreach (var root in roots)
					{
						root.GetSharedMaterials(tempMaterials);

						for (var materialIndex = 0; materialIndex < tempMaterials.Count; materialIndex++)
						{
							var material = tempMaterials[materialIndex];

							if (paintables.Exists(p => p.Target == root) == false)
							{
								TryExport(root, materialIndex, material);
							}
							else
							{
								foreach (var paintable in paintables)
								{
									if (paintable.Target == root)
									{
										ExportTexture(paintable);
									}
								}
							}
						}
					}
				}
			}

			for (var i = paintables.Count - 1; i >= 0; i--)
			{
				paintables[i].Clear();

				paintables.RemoveAt(i);
			}
		}

		protected virtual void OnGUI()
		{
			CwEditor.ClearStacks();

			EditorGUI.BeginChangeCheck();
			{
				Draw();
			}
			if (EditorGUI.EndChangeCheck() == true)
			{
				Repaint();
			}

			foreach (var p in paintables)
			{
				if (p.Target != null)
				{
					var source = TryGetSource(p.Target);

					if (p.TargetSource != source)
					{
						if (TryGetPrepared(p.Target) == true)
						{
							DestroyImmediate(p.paintMesh);

							p.TargetSource = preparedSource;
							p.paintMesh    = CwEditorMesh.GetProcessedCopy(preparedMesh, p.SlotChannel);
						}
					}
				}
			}
		}

		protected virtual void OnSelectionChange()
		{
			Repaint();
		}

		private void Draw()
		{
			var previousPage = CurrentPage;

			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
				EditorGUI.BeginDisabledGroup(CanUndo == false);
					if (GUILayout.Button(new GUIContent("↺", "Undo"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)) == true)
					{
						UndoAll();
					}
				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(CanRedo == false);
					if (GUILayout.Button(new GUIContent("↻", "Redo"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)) == true)
					{
						RedoAll();
					}
				EditorGUI.EndDisabledGroup();
				if (GUILayout.Toggle(CurrentPage == PageType.Objects, "Objects", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)) == true)
				{
					CurrentPage = PageType.Objects;
				}

				if (GUILayout.Toggle(CurrentPage == PageType.Paint, "Paint", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)) == true)
				{
					CurrentPage = PageType.Paint;
				}

				if (GUILayout.Toggle(CurrentPage == PageType.Config, "Config", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)) == true)
				{
					CurrentPage = PageType.Config;
				}
				EditorGUILayout.Separator();
			EditorGUILayout.EndHorizontal();

			switch (CurrentPage)
			{
				case PageType.Objects:
				{
					DrawObjectsTab();
				}
				break;

				case PageType.Paint:
				{
					DrawPaintTab();
				}
				break;

				case PageType.Config:
				{
					DrawConfigTab();
				}
				break;
			}

			if (CurrentPage == PageType.Paint)
			{
				CwEditorTool.SelectThisTool();
			}
			else
			{
				CwEditorTool.DeselectThisTool();
			}
		}

		private string GetExtension(ExportTextureFormat f)
		{
			switch (Settings.Format)
			{
				case ExportTextureFormat.PNG: return "png";
				case ExportTextureFormat.TGA: return "tga";
			}

			return null;
		}

		private byte[] GetData(CwEditorPaintable t, string path)
		{
			var f = default(ExportTextureFormat);

			if (path.EndsWith("png", System.StringComparison.InvariantCultureIgnoreCase) == true)
			{
				f = ExportTextureFormat.PNG;
			}
			else if (path.EndsWith("tga", System.StringComparison.InvariantCultureIgnoreCase) == true)
			{
				f = ExportTextureFormat.TGA;
			}

			return GetData(t, f);
		}

		private byte[] GetData(CwEditorPaintable p, ExportTextureFormat f)
		{
			var t = default(Texture2D);

			p.FullDilate();

			if (p.TextureProfile.Export == CwEditorTextureProfile.ExportType.sRGB)
			{
				t = GetReadableCopy(p.Current, true);
			}
			else
			{
				t = GetReadableCopy(p.Current, true);
			}

			var d = default(byte[]);

			switch (Settings.Format)
			{
				case ExportTextureFormat.PNG: d = t.EncodeToPNG(); break;
				case ExportTextureFormat.TGA: d = t.EncodeToTGA(); break;
			}

			DestroyImmediate(t);

			return d;
		}

		public static Texture2D GetReadableCopy(RenderTexture renderTexture, bool convert)
		{
			var newTexture = default(Texture2D);
			var oldActive  = RenderTexture.active;

			if (renderTexture != null)
			{
				var copy = new RenderTexture(renderTexture.width, renderTexture.height, 0, RenderTextureFormat.ARGB32);

				Graphics.Blit(renderTexture, copy); RenderTexture.active = oldActive;

				newTexture = new Texture2D(copy.width, copy.height, TextureFormat.ARGB32, false);

				// Read
				CwHelper.BeginActive(copy);
					newTexture.ReadPixels(new Rect(0, 0, copy.width, copy.height), 0, 0);
				CwHelper.EndActive();

				if (convert == true && PlayerSettings.colorSpace == ColorSpace.Linear)
				{
					Color[] pixels = newTexture.GetPixels();
					for (int p = 0; p < pixels.Length; p++)
					{
						pixels[p] = pixels[p].linear;
					}
					newTexture.SetPixels(pixels);
				}

				newTexture.Apply();

				// Cleanup
				copy.Release(); DestroyImmediate(copy);
			}

			return newTexture;
		}

		private Texture2D WriteTextureDataAndLoad(CwEditorPaintable paintable, string path)
		{
			var data = GetData(paintable, Settings.Format);

			System.IO.File.WriteAllBytes(path, data);

			AssetDatabase.ImportAsset(path);

			var importer = (TextureImporter)AssetImporter.GetAtPath(path);

			if (paintable.TextureProfile.Export == CwEditorTextureProfile.ExportType.sRGB)
			{
				importer.sRGBTexture = true;
			}

			if (paintable.TextureProfile.Export == CwEditorTextureProfile.ExportType.Normal)
			{
				importer.textureType = TextureImporterType.NormalMap;
			}

			importer.SaveAndReimport();

			paintable.NeedsExporting = false;

			return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		}

		private static GUIStyle selectableStyleA;
		private static GUIStyle selectableStyleB;
		private static GUIStyle selectableStyleC;

		private static Texture2D LoadTempTexture(string base64)
		{
			var tex  = new Texture2D(1, 1);
			var data = System.Convert.FromBase64String(base64);

			tex.LoadImage(data);

			return tex;
		}

		private static GUIStyle GetSelectableStyle(bool selected, bool pad)
		{
			if (selectableStyleA == null || selectableStyleA.normal.background == null)
			{
				selectableStyleA = new GUIStyle(); selectableStyleA.border = new RectOffset(4,4,4,4); selectableStyleA.normal.background = LoadTempTexture("iVBORw0KGgoAAAANSUhEUgAAAAgAAAAICAYAAADED76LAAAASUlEQVQYGWN0iL73nwEPYIHKNeJQUw9TAJI/gKbIAcRnQhbcv0TxAAgji6EoQJaAsZGtYHCMue8Ak4DRyAowJEGKYArqYTrQaQBpfAuV0+TyawAAAABJRU5ErkJggg==");
			}

			if (selectableStyleB == null || selectableStyleB.normal.background == null)
			{
				selectableStyleB = new GUIStyle(); selectableStyleB.border = new RectOffset(4,4,4,4); selectableStyleB.normal.background = LoadTempTexture("iVBORw0KGgoAAAANSUhEUgAAAAgAAAAICAYAAADED76LAAAARElEQVQYGWNkYGBwAGKcgAUq8wCHCgWYApD8BzRFAiA+E5ogSBGKQnQFaOoZGJCtAEmCjUVWhawAQxKkEKZAAVkXMhsAA6sEekpg61oAAAAASUVORK5CYII=");
			}

			if (selectableStyleC == null)
			{
				selectableStyleC = new GUIStyle(selectableStyleA); selectableStyleC.padding = new RectOffset(2,2,4,4);
			}

			if (selected == true)
			{
				return pad == true ? selectableStyleC : selectableStyleA;
			}

			return selectableStyleB;
		}

		private static float LogSlider(string title, float value, float logMin, float logMax)
		{
			var rect   = CwEditor.Reserve();
			var rectA  = rect; rectA.width = EditorGUIUtility.labelWidth + 50;
			var rectB  = rect; rectB.xMin += EditorGUIUtility.labelWidth + 52;
			var logOld = Mathf.Log10(value);
			var logNew = GUI.HorizontalSlider(rectB, logOld, logMin, logMax);

			if (logOld != logNew)
			{
				value = Mathf.Pow(10.0f, logNew);
			}

			return EditorGUI.FloatField(rectA, title, value);
		}

		private static float Slider(string title, float value, float min, float max)
		{
			var rect  = CwEditor.Reserve();
			var rectA = rect; rectA.width = EditorGUIUtility.labelWidth + 50;
			var rectB = rect; rectB.xMin += EditorGUIUtility.labelWidth + 52;

			value = GUI.HorizontalSlider(rectB, value, min, max);

			return EditorGUI.FloatField(rectA, title, value);
		}

		public static bool IntersectRayMesh(Ray ray, MeshFilter meshFilter, out RaycastHit hit)
		{
			return IntersectRayMesh(ray, meshFilter.mesh, meshFilter.transform.localToWorldMatrix, out hit);
		}

		public static bool IntersectRayMesh(Ray ray, Mesh mesh, Matrix4x4 matrix, out RaycastHit hit)
		{
			var parameters = new object[] { ray, mesh, matrix, null };
			var result     = (bool)method_IntersectRayMesh.Invoke(null, parameters);

			hit = (RaycastHit)parameters[3];

			return result;
		}
	}
}