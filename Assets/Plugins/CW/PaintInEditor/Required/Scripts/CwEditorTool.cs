using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using CW.Common;
using System;

namespace PaintInEditor
{
	/// <summary>This is the custom editor tool used to paint in the Scene view.</summary>
	[EditorTool("Paint in Editor")]
	public class CwEditorTool : EditorTool
	{
		public static Camera LastCamera;

		public static double OldDistance;

		public static Vector2 OldPoint;

		public static Vector2 NewPoint;

		public static float LastPressure;

		public static bool Set;

		public static bool LastValid;

		public static RenderTexture LastDepthTexture;

		public static Matrix4x4 LastDepthMatrix;

		private static Matrix4x4 lastMatrix;

		private static Texture cursorTexture;

		private static Vector2 cursorSize;

		private static float cursorAngle;

		public static event System.Action<SceneView> OnToolUpdate;

		[System.NonSerialized]
		private Camera depthCamera;

		private static GUIContent cachedDarkContent;

		private static GUIContent cachedLightContent;

		public override GUIContent toolbarIcon
		{
			get
			{
				if (EditorGUIUtility.isProSkin == true)
				{
					if (cachedDarkContent == null || cachedDarkContent.image == null)
					{
						var t = new Texture2D(1, 1);

						t.LoadImage(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAAEo0lEQVR4Ad1ZO2gVQRRNoqJBoqgQsRG1UDQKWgQ0+ANBG20sFARbRfz/8dsYxW+hBhV7wSaVoqAWFiZq5V+CiIiiYLDwm/iLes5L3stk387uzNzdfbt74eTtztw7c8+Zmd3ZSXVVzNbe3h5zD7Lmq2XhwdFpJ8/sYxMgC+RjEyAr5GMRIEvkIxUga8RJnhbJMyCr5CMRIMvkxQJknbxIgDyQdxYgL+SdBMgTeWsBUkK+AYmvAOYDdcB74AZwGfgIWJnxazAl5NeD3SFglA/LxyjbAdz0qdMWGQmQEvJ7weKwlklvRRd+FgNtIX6l6kGlK81FSsgfRHoc+TAbAodG4A7QGebM+kABUkJ+N/IMG3mVaz1ueoDraqHuukZXkRLy+5DfUV2OmnIu6zmaurJiXwFSQp4j31yWsVlBrZlbVVWZACkhvxMEbEde5fxLvQm6HiBASsjzVXY8KGmDumcGPgWXkgApIc+RP2aavMbvLcpbNHVlxQUBUkJ+D7LjyJcGpSzb8IIfcOFm6X64a69HTUrIb0A6R0yT1vj9Rvk24Iqm3rdYorZvgw6FuxBz1iFODSF5inheLTS5rrQAHDHpyP9EG1uAiyaEvT6DvQUJ3nOtnhL2xx0f3xrnXNup1AzgiBk/qTXk/qB8k7SdSgjAkZe+5/+hja2A88gjtmBJC7ARvfKBx682V+OaJ3npDCr0n6QA69Aj17zRGUQhO/8//EA67V9lX5qEAMOR1gGA01Uy8pz2mwHpgxNN9FsSAkxEd1OAVuBhf9dWV3zgcb9wxirKwDmJ12AH8ljdl8sY/M4GSGZeX5nJz344nTRxtPVJYgZw9Dh9CZ7aXgWWACZvgr/wi+IDCc34WxwzYDy6agBGAPw4eQk8ByhA0bpxwQMPHsltLxb6/MY28sW+ohRgFhrlrmwZwAcfZxdJ83DiHsARvwaoxofjTGCRWohrjrzLcZinmfDbqJbAWnR1C1gF1AHFdvnKGwosADj1+QQfBhSNM4F7eC4T1XgKLDkRUtsKvC4mGugUUrkG9ReA0SF+FIMfP80eP84KLhMaZwynvc0pMOOcTSrAXPRs8jBTE+SaX6oUfMP10757LonEyLNPiQDc1DDhkWzI0vgRo+4IueY5ixIlz5wlD8EZiOe/oVxsOoKmAnw70LgsjA8yCxER/ZHMgIWCHMYidoIS/wTXnAWJm0SAcYJs2W+tID6yUIkA3leXTVKM/W4TEIdvU1OT6CH4RZDUO8S+EMSLQ0meJpkBXYIsHiH2lSBeFFokz0YkAriuYR5h2+4dRISDgiUC8IPH1rjT40dQm21gVP7q6LNNVwH4pddomRRfcyQf+aGGZR4D3F0FWI5WJg9oKfiGW92VwAmgJ9g1vlrv6LMnl51gPeK4BVa3smzLzzpQeAloAT75OSRV5keefdsKwI8fkpnEYI9xin8FOoHbQCvAM8APQEVNR55J2QowDTEc+bsMhnE6dwOfgTcAt7QPgNcAyypuQeSZXHVK/j0ei1Bh5Nmp60MwloSjbNSEfG4FMCWfSwFsyOdOAFvyuRLAhXxuBHAlnwsBJOQzL4CUfKYFiII8BfgPInzXEgIu3JIAAAAASUVORK5CYII="));

						cachedDarkContent = new GUIContent(t);
					}

					return cachedDarkContent;
				}
				else
				{
					if (cachedLightContent == null || cachedLightContent.image == null)
					{
						var t = new Texture2D(1, 1);

						t.LoadImage(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAAEoElEQVR4Ad1ZSWgUQRRNoqJBoqgQ8SLqQdEo6CGg4AaCXvQiQUHwqoj7juvFKK4HNah4FzwkJ0VBPXgwqCd3CSIiioLBg2viFvW9SXpSmemlqn53T3V/eJnuqv+r/ntV1V1dqa5K2JqamhLuQdZ8tSw8PNp18sw+MQGyQD4xAbJCPhEBskQ+VgGyRpzkabE8A7JKPhYBskxeLEDWyYsEyAN5awHyQt5KgDyRNxbAEfINSHwFMB+oA94DN4DLwEfAyLRfg46QXw92h4BRPiwfo2wHcNOnLrBISwBHyO8Fi8OBTHoruvCzGGiP8CtWDypeBVw4Qv4g0uPIR9kQODQCd4DOKGfWhwrgCPndyDNq5FWu9bjpAa6rhUHXNUEVjpDfh/yOBuUYUM5lPSegrqzYVwBHyHPkm8sy1iuo1XOrqioTwBHyO0HAdORVzr/Um7DrAQI4Qp6vsuNhSWvUPdPwKbgUBXCEPEf+mG7yAX5vUd4SUFdWXBDAEfJ7kB1HvjgoZdlGF/yACzdL96Ndez1qHCG/Aekc0U06wO83yrcBVwLqfYslavs2aFG4CzFnLeLUEJKniOfVQp3rSgvAEZOO/E+0sQW4qEO41GdwaUGK91yrp4T9ccfHt8Y523YqNQM4YtpP6gByf1C+SdpOJQTgyEvf8//QxlbAeuQRW7C0BdiIXvnA41ebrXHNk7x0BhX6T1OAdeiRa17rDKKQnf8ffiCd9q8yL01DgOFI6wDA6SoZeU77zYD0wYkm+i0NASaiuylAG/Cwv2ujKz7wuF84YxSl4ZzGa7ADeazuy2UMfmcDJDOvr0znZz+cTuo4mvqkMQM4epy+BE9trwJLAJ03wV/4xfGBhGb8LYkZMB5dNQAjAH6cvASeAxTAs25c8MCDR3LbvUKf38RG3usrTgFmoVHuypYBfPBxdpE0DyfuARzxa4BqfDjOBBaphbjmyNsch5U0E30b1xJYi65uAauAOsBrl6+8ocACgFOfT/BhgGecCdzDc5moxlNgyYmQ2lbotZdoqFNE5RrUXwBGR/hRDH78NJf4cVZwmdA4YzjtTU6BGWdtUgHmomedh5maINf8UqXgG66f9t1zSaRGnn1KBOCmhgmPZEOGxo8YdUfINc9ZlCp55ix5CM5APP8NZWPTETQV4NuBxmWhfZBZiIjpj2QGLBTkMBaxE5T4J7jmLEjdJAKME2TLfmsF8bGFSgQofXWZJMXY7yYBSfi2traKHoJfBEm9Q+wLQbw4lORpkhnQJcjiEWJfCeJFoR55NiIRwHYN8wjbdO8gIhwWLBGAHzymxp0eP4LaTQPj8ldHn23aCsAvvUbDpPiaI/nYDzUM8xjgbivAcrQyeUBL4Tfc6q4ETgA94a7J1ZaOPnuy2QnWI45bYHUry7b8rAOFl4AW4JOfQ1plfuTZt6kA/PghmUkMLjFO8a9AJ3AbaAN4BvgBqKgFkWdSpgJMQwxH/i6DYZzO3cBn4A3ALe0D4DXAsopbGHkmZyoADy+ITFgUeZKwfQg6L4AO+dwKoEs+lwKYkM+dAKbkcyWADfncCGBLPhcCSMhnXgAp+UwLEAd5CvAfzTzN8wY/ubcAAAAASUVORK5CYII="));

						cachedLightContent = new GUIContent(t);
					}

					return cachedLightContent;
				}
			}
		}

		static CwEditorTool()
		{
			Camera.onPreRender += (camera) => CwPaintInEditor.ApplyPaintables();

			Camera.onPostRender += (camera) => CwPaintInEditor.RemovePaintables();

			UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += (context, camera) => CwPaintInEditor.ApplyPaintables();

			UnityEngine.Rendering.RenderPipelineManager.endCameraRendering += (context, camera) => CwPaintInEditor.RemovePaintables();
		}

		public override bool IsAvailable()
		{
			return true;
		}

		public static Ray GetRay(Vector2 screenPosition)
		{
			if (LastCamera != null)
			{
				return LastCamera.ScreenPointToRay(screenPosition);
			}

			return default(Ray);
		}

		public static void SelectThisTool()
		{
			if (ToolManager.activeToolType != typeof(CwEditorTool))
			{
				Tools.current = Tool.Custom;

				ToolManager.SetActiveTool<CwEditorTool>();
			}
		}

		public static void DeselectThisTool()
		{
			if (ToolManager.activeToolType == typeof(CwEditorTool))
			{
				ToolManager.RestorePreviousTool();

				if (ToolManager.activeToolType == typeof(CwEditorTool))
				{
					Tools.current = Tool.Move;
				}
			}
		}

		public static Matrix4x4 GetMatrix(Vector2 point)
		{
			var coordA = GetPaintCoord(point);
			var coordB = GetPaintCoord(point + cursorSize * 0.5f * EditorGUIUtility.pixelsPerPoint);

			return SubMatrix(coordA, coordB - coordA) * lastMatrix;
		}

		private void OpenAndSelectPaint(bool init)
		{
			CwPaintInEditor.CurrentPage = CwPaintInEditor.PageType.Paint;

			if (EditorWindow.HasOpenInstances<CwPaintInEditor>() == false)
			{
				CwPaintInEditor.OpenWindow();

				if (init == true)
				{
					var instance = CwPaintInEditor.GetWindow();

					if (instance != null)
					{
						instance.AutoLockSelection();
					}
				}
			}

			CwPaintInEditor.TryRepaint();
		}

		public override void OnActivated()
		{
			base.OnActivated();

			OpenAndSelectPaint(true);
		}

		protected virtual void OnDisable()
		{
			ClearDepthCamera();

			DeselectThisTool();
		}

		private void CreateDepthCamera()
		{
			ClearDepthCamera();

			var root = new GameObject("DepthCamera");

			root.hideFlags = HideFlags.HideAndDontSave;

			depthCamera = root.AddComponent<Camera>();
			depthCamera.enabled = false;
		}

		private void ClearDepthCamera()
		{
			if (depthCamera != null)
			{
				DestroyImmediate(depthCamera.gameObject);

				depthCamera = null;
			}

			LastDepthTexture = CwHelper.Destroy(LastDepthTexture);
		}

		public override void OnToolGUI(EditorWindow window)
		{
			var isMouseEvent = Event.current.type == EventType.MouseMove || Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseUp;

			if (Event.current.type == EventType.ScrollWheel && Event.current.delta.y != 0.0f)
			{
				if (CwPaintInEditor.HandleScrollWheel(Event.current, Event.current.delta.y < 0.0f) == true)
				{
					CwPaintInEditor.TryRepaint();

					Event.current.Use();
				}
			}

			OldDistance += Vector2.Distance(OldPoint, NewPoint);

			OldPoint = NewPoint;

			OpenAndSelectPaint(false);

			var sceneView = window as SceneView;

			if (sceneView != null)
			{
				HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

				LastCamera   = sceneView.camera;
				LastPressure = Event.current.pressure;

				if (isMouseEvent == true)
				{
					NewPoint = Event.current.mousePosition;
				}

				var modifiersMask = EventModifiers.Shift | EventModifiers.Control | EventModifiers.Alt | EventModifiers.Command;
				var modifiers     = Event.current.modifiers & modifiersMask;

				if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && modifiers == 0)
				{
					if (Set == false)
					{
						OldDistance = 0.0f;
						OldPoint    = NewPoint;
					}

					Set = true;
				}

				if (Event.current.type == EventType.MouseUp)
				{
					Set = false;
				}

				if (OnToolUpdate != null)
				{
					OnToolUpdate.Invoke(sceneView);
				}

				LastValid = EditorWindow.mouseOverWindow != null && EditorWindow.mouseOverWindow is SceneView;

				if (Set == true || modifiers == 0)
				{
					if (CwPaintInEditor.GetCursorShape(ref cursorTexture, ref cursorSize, ref cursorAngle) == true)
					{
						lastMatrix = sceneView.camera.projectionMatrix * sceneView.camera.worldToCameraMatrix;

						Handles.BeginGUI();
							CwPaintInEditor.OutlineMaterial.SetTexture("_CwShapeTex", cursorTexture);
							CwPaintInEditor.OutlineMaterial.SetVector("_CwShapeChannel", CwCommon.GetTextureChannel(cursorTexture));

							var oldMatrix = GUI.matrix;
							GUI.matrix = Matrix4x4.Rotate(Quaternion.Euler(0.0f, 0.0f, cursorAngle)) * GUI.matrix;
								EditorGUI.DrawPreviewTexture(new Rect(Event.current.mousePosition.x - cursorSize.x * 0.5f,Event.current.mousePosition.y - cursorSize.y* 0.5f, cursorSize.x, cursorSize.y), cursorTexture, CwPaintInEditor.OutlineMaterial);
							GUI.matrix = oldMatrix;
						Handles.EndGUI();
					}
				}

				if (Event.current.type == EventType.Layout)
				{
					UpdateDepth(sceneView.camera);
				}

				if (isMouseEvent == true)
				{
					sceneView.Repaint();
				}
			}
		}

		private void UpdateDepth(Camera sourceCamera)
		{
			if (depthCamera == null)
			{
				CreateDepthCamera();
			}

			if (LastDepthTexture == null)
			{
				LastDepthTexture = new RenderTexture(64, 64, 32, RenderTextureFormat.Depth);
			}

			if (sourceCamera != null && LastDepthTexture != null)
			{
				var newWidth  = sourceCamera.pixelWidth;
				var newHeight = sourceCamera.pixelHeight;

				if (LastDepthTexture.width != newWidth || LastDepthTexture.height != newHeight)
				{
					if (LastDepthTexture.IsCreated() == true)
					{
						LastDepthTexture.Release();
					}

					LastDepthTexture.width  = newWidth;
					LastDepthTexture.height = newHeight;

					LastDepthTexture.Create();
				}

				LastDepthMatrix = Matrix4x4.Translate(new Vector3(0.5f, 0.5f, 0.5f)) * Matrix4x4.Scale(new Vector3(0.5f, 0.5f, 0.5f)) * sourceCamera.projectionMatrix * sourceCamera.worldToCameraMatrix;

				Shader.SetGlobalMatrix("_CwDepthMatrix", LastDepthMatrix);

				depthCamera.CopyFrom(sourceCamera);

				depthCamera.enabled         = false;
				depthCamera.clearFlags      = CameraClearFlags.SolidColor;
				depthCamera.backgroundColor = Color.black;
				depthCamera.targetTexture   = LastDepthTexture;

				depthCamera.transform.position = sourceCamera.transform.position;
				depthCamera.transform.rotation = sourceCamera.transform.rotation;

				depthCamera.RenderWithShader(CwPaintInEditor.DepthMaterial.shader, "RenderType");
			}
		}

		private static Vector2 GetPaintCoord(Vector2 mousePosition)
		{
			var u = (       mousePosition.x / LastCamera.pixelRect.width ) * 2.0f - 1.0f;
			var v = (1.0f - mousePosition.y / LastCamera.pixelRect.height) * 2.0f - 1.0f;

			return new Vector2(u, v);
		}

		public static Matrix4x4 SubMatrix(Vector2 center, Vector2 size)
		{
			return Matrix4x4.Scale(new Vector3(CwHelper.Reciprocal(size.x), CwHelper.Reciprocal(size.y), 1.0f)) * Matrix4x4.Scale(new Vector3(1.0f, -1.0f, 1.0f)) * Matrix4x4.Translate(-center);
		}
	}
}