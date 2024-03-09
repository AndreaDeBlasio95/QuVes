using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using CW.Common;

namespace PaintInEditor
{
	public partial class CwPaintInEditor
	{
		enum PaintPageType
		{
			Main,
			BrushBrowser,
			ShapeBrowser
		}

		private PaintPageType    currentPaintPage;
		private Vector2          paintScrollPosition;
		private Vector2          paintBrushScrollPosition;
		private Vector2          paintShapeScrollPosition;
		private string           paintBrushFilter;
		private string           paintShapeFilter;
		private List<IBrowsable> browserItems = new List<IBrowsable>();

		private static bool lastRaySet;

		private static Mesh bakedMesh;

		private static int _CwOffset      = Shader.PropertyToID("_CwOffset");
		private static int _CwBuffer      = Shader.PropertyToID("_CwBuffer");
		private static int _CwBufferSize  = Shader.PropertyToID("_CwBufferSize");

		private static CwEditorPaintable preparedPaintable;
		private static Matrix4x4         preparedMatrix;
		private static Mesh              preparedMesh;
		private static Object            preparedSource;
		private static bool              preparedState;
		private static List<Vector3>     preparedVertices  = new List<Vector3>();
		private static List<Vector2>     preparedCoords    = new List<Vector2>();
		private static List<ushort>      preparedTriangles = new List<ushort>();

		public static void PaintNow(Material material)
		{
			if (material != null)
			{
				var swap = CwCommon.GetRenderTexture(preparedPaintable.Current);

				Graphics.Blit(preparedPaintable.Current, swap);

				material.SetTexture(_CwBuffer, swap);
				material.SetVector(_CwBufferSize, new Vector2(swap.width, swap.height));

				RenderTexture.active = preparedPaintable.Current;

				if (material.SetPass(0) == true)
				{
					Graphics.DrawMeshNow(preparedPaintable.paintMesh, preparedMatrix, 0);
				}

				CwCommon.ReleaseRenderTexture(swap);

				foreach (var paintable in paintables)
				{
					paintable.NeedsExporting = true;
				}
			}
		}

		private void HandleToolUpdate(SceneView sceneView)
		{
			preparedState = false;

			if (CurrentPage == PageType.Paint && CwEditorTool.Set == true)
			{
				if (Settings.Brush != null)
				{
					var oldActive = RenderTexture.active;
					var oldPoint  = CwEditorTool.OldPoint;
					var newPoint  = CwEditorTool.NewPoint;
					var seed      = Random.Range(int.MinValue, int.MaxValue);

					oldPoint = (oldPoint - CwEditorTool.LastCamera.pixelRect.position) * EditorGUIUtility.pixelsPerPoint;
					newPoint = (newPoint - CwEditorTool.LastCamera.pixelRect.position) * EditorGUIUtility.pixelsPerPoint;

					foreach (var paintable in paintables)
					{
						if (TryGetPrepared(paintable.Target) == true)
						{
							preparedPaintable = paintable;

							foreach (var subset in paintable.TextureProfile.Subsets)
							{
								CwHelper.BeginSeed(seed);

									Settings.Brush.DrawBrush(Settings, paintable, subset, CwEditorTool.OldDistance, oldPoint, newPoint, lastRaySet == false);

								CwHelper.EndSeed();
							}

							if (Settings.Dilate == true)
							{
								paintable.Dilate(Settings.DilateSteps);
							}

							if (paintable.Current.useMipMap == true && paintable.Current.autoGenerateMips == false)
							{
								paintable.Current.GenerateMips();
							}
						}
					}

					RenderTexture.active = oldActive;
				}

				lastRaySet = true;
			}
			else
			{
				lastRaySet = false;
			}
		}

		private static Object TryGetSource(Renderer target)
		{
			var tt = target.transform;
			var mf = tt.GetComponent<MeshFilter>();

			if (mf != null)
			{
				return mf.sharedMesh;
			}
			
			var smr = tt.GetComponent<SkinnedMeshRenderer>();

			if (smr != null)
			{
				return smr.sharedMesh;
			}

			var sr = tt.GetComponent<SpriteRenderer>();

			if (sr != null)
			{
				return sr.sprite;
			}

			return null;
		}

		private static bool TryGetPrepared(Renderer target)
		{
			var tt = target.transform;
			var mf = tt.GetComponent<MeshFilter>();

			if (mf != null)
			{
				preparedMesh   = mf.sharedMesh;
				preparedSource = mf.sharedMesh;
				preparedMatrix = tt.localToWorldMatrix;

				return true;
			}

			var smr = tt.GetComponent<SkinnedMeshRenderer>();

			if (smr != null)
			{
				if (bakedMesh == null)
				{
					bakedMesh = new Mesh();
				}

				var lossyScale    = tt.lossyScale;
				var scaling       = new Vector3(CwHelper.Reciprocal(lossyScale.x), CwHelper.Reciprocal(lossyScale.y), CwHelper.Reciprocal(lossyScale.z));
				var oldLocalScale = tt.localScale;

				tt.localScale = Vector3.one;

				smr.BakeMesh(bakedMesh);

				tt.localScale = oldLocalScale;

				preparedMesh   = bakedMesh;
				preparedSource = smr.sharedMesh;
				preparedMatrix = tt.localToWorldMatrix;

				//if (includeScale == true)
				{
					preparedMatrix *= Matrix4x4.Scale(scaling);
				}

				return true;
			}

			var sr = tt.GetComponent<SpriteRenderer>();

			if (sr != null)
			{
				var sprite = sr.sprite;

				if (sprite != null)
				{
					preparedVertices.Clear();
					preparedCoords.Clear();
					preparedTriangles.Clear();

					var spriteV = sprite.vertices;
					var spriteC = sprite.uv;
					var spriteT = sprite.triangles;

					foreach (var v in spriteV) preparedVertices.Add(v);
					preparedCoords.AddRange(spriteC);
					preparedTriangles.AddRange(spriteT);

					/*
					foreach (var v in spriteV) preparedVertices.Add(v);
					preparedCoords.AddRange(spriteC);
					for (var t = 0; t < spriteT.Length; t += 3)
					{
						preparedTriangles.Add((ushort)(spriteT[t + 2] + spriteV.Length));
						preparedTriangles.Add((ushort)(spriteT[t + 1] + spriteV.Length));
						preparedTriangles.Add((ushort)(spriteT[t + 0] + spriteV.Length));
					}
					*/

					if (bakedMesh == null)
					{
						bakedMesh = new Mesh();
					}
					else
					{
						bakedMesh.Clear();
					}

					bakedMesh.SetVertices(preparedVertices);
					bakedMesh.SetUVs(0, preparedCoords);
					bakedMesh.SetTriangles(preparedTriangles, 0);
					bakedMesh.RecalculateBounds();
					bakedMesh.RecalculateNormals();
					bakedMesh.RecalculateTangents();

					preparedMesh   = bakedMesh;
					preparedSource = sprite;
					preparedMatrix = sr.localToWorldMatrix;

					return true;
				}
			}

			return false;
		}

		private void DrawPaintTab()
		{
			switch (currentPaintPage)
			{
				case PaintPageType.Main:
				{
					DrawPaintTab_Main();
				}
				break;

				case PaintPageType.BrushBrowser:
				{
					DrawPaintTab_BrushBrowser();
				}
				break;

				case PaintPageType.ShapeBrowser:
				{
					DrawPaintTab_ShapeBrowser();
				}
				break;
			}
		}

		private void DrawPaintTab_Footer(ref string filter)
		{
			EditorGUILayout.Separator();

			CwEditor.BeginLabelWidth(50);
				filter = EditorGUILayout.TextField("Filter", filter);
			CwEditor.EndLabelWidth();
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
				Settings.IconSize = EditorGUILayout.IntSlider(Settings.IconSize, 32, 256);

				EditorGUILayout.Separator();

				if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)) == true)
				{
					CwEditorBrush_Editor.ClearCache(); AssetDatabase.Refresh();
				}

				if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)) == true)
				{
					currentPaintPage = PaintPageType.Main;
				}
			EditorGUILayout.EndHorizontal();
		}

		private void DrawPaintTab_Main()
		{
			var brushIcon  = default(Texture);
			var brushTitle = "None";
			var shapeIcon  = default(Texture);
			var shapeTitle = "None";
			var width      = Mathf.FloorToInt((position.width - 30) / 2);

			if (Settings.Brush != null)
			{
				brushIcon  = Settings.Brush.GetIcon();
				brushTitle = Settings.Brush.GetTitle();
			}

			if (Settings.Wallpaper.Shape != null)
			{
				shapeIcon  = Settings.Wallpaper.Shape.GetIcon();
				shapeTitle = Settings.Wallpaper.Shape.GetTitle();
			}

			var brushNeedsShape = Settings.Brush != null && Settings.Brush.NeedsShape;

			var rectA = default(Rect);
			var rectB = default(Rect);

			EditorGUILayout.Separator();
			EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				EditorGUILayout.BeginVertical();
					rectA = DrawIcon(width, brushIcon, brushTitle, Settings.Brush != null, "Brush");
				EditorGUILayout.EndVertical();
				if (brushNeedsShape == true)
				{
					GUILayout.FlexibleSpace();
					EditorGUILayout.BeginVertical();
						rectB = DrawIcon(width, shapeIcon, shapeTitle, Settings.Wallpaper.Shape != null, "Shape");
					EditorGUILayout.EndVertical();
				}
				GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			if (Event.current.type == EventType.MouseDown && rectA.Contains(Event.current.mousePosition) == true)
			{
				if (Event.current.button == 0)
				{
					currentPaintPage = PaintPageType.BrushBrowser; Repaint();
				}
				else
				{
					CwHelper.SelectAndPing(Settings.Brush);
				}
			}

			if (Event.current.type == EventType.MouseDown && rectB.Contains(Event.current.mousePosition) == true && brushNeedsShape == true)
			{
				if (Event.current.button == 0)
				{
					currentPaintPage = PaintPageType.ShapeBrowser; Repaint();
				}
				else
				{
					CwHelper.SelectAndPing(Settings.Wallpaper.Shape);
				}
			}

			EditorGUILayout.Separator();

			if (HasPaintables == false)
			{
				CwEditor.Warning("You need to configure objects for painting. Please go to the <b>Objects</b> tab.");
			}

			paintScrollPosition = GUILayout.BeginScrollView(paintScrollPosition, GUILayout.ExpandHeight(true));
				if (Settings.Brush != null)
				{
					Settings.Brush.DrawSettings(Settings);
				}
			GUILayout.EndScrollView();
		}

		private void PopulateBrowserItems<T>(List<T> items, string filter)
			where T : IBrowsable
		{
			browserItems.Clear();
			
			foreach (var item in items)
			{
				if (item != null)
				{
					if (string.IsNullOrEmpty(filter) == true || CwCommon.ContainsIgnoreCase(item.GetTitle(), filter) == true)
					{
						browserItems.Add(item);
					}
				}
			}
		}

		private void DrawPaintTab_BrushBrowser()
		{
			GUILayout.Label("Select a Brush", GetTitleBold(), GUILayout.ExpandWidth(true));

			PopulateBrowserItems(CwEditorBrush_Editor.CachedInstances, paintBrushFilter);

			paintBrushScrollPosition = GUILayout.BeginScrollView(paintBrushScrollPosition, GUILayout.ExpandHeight(true));

			var selected = DrawBrowser(browserItems, Settings.Brush);

			if (selected != null)
			{
				Settings.Brush = (CwEditorBrush)selected;

				currentPaintPage = PaintPageType.Main; Repaint();
			}

			GUILayout.EndScrollView();

			DrawPaintTab_Footer(ref paintBrushFilter);
		}

		private void DrawPaintTab_ShapeBrowser()
		{
			GUILayout.Label("Select a Shape", GetTitleBold(), GUILayout.ExpandWidth(true));

			PopulateBrowserItems(CwEditorShape_Editor.CachedInstances, paintShapeFilter);

			paintShapeScrollPosition = GUILayout.BeginScrollView(paintShapeScrollPosition, GUILayout.ExpandHeight(true));

			var selected = DrawBrowser(browserItems, Settings.Wallpaper.Shape);

			if (selected != null)
			{
				Settings.Wallpaper.Shape = (CwEditorShape)selected;

				currentPaintPage = PaintPageType.Main; Repaint();
			}

			GUILayout.EndScrollView();

			DrawPaintTab_Footer(ref paintShapeFilter);
		}
	}
}