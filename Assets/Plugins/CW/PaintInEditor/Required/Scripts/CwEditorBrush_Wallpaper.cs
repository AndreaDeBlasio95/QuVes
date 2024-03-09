using CW.Common;
using UnityEditor;
using UnityEngine;

namespace PaintInEditor
{
	public class CwEditorBrush_Wallpaper : CwEditorBrush
	{
		public Texture AlbedoTexture { set { albedoTexture = value; } get { return albedoTexture; } } [SerializeField] private Texture albedoTexture;

		public Texture OpacityTexture { set { opacityTexture = value; } get { return opacityTexture; } } [SerializeField] private Texture opacityTexture;

		public Texture NormalTexture { set { normalTexture = value; } get { return normalTexture; } } [SerializeField] private Texture normalTexture;

		public Texture MetallicTexture { set { metallicTexture = value; } get { return metallicTexture; } } [SerializeField] private Texture metallicTexture;

		public Texture OcclusionTexture { set { occlusionTexture = value; } get { return occlusionTexture; } } [SerializeField] private Texture occlusionTexture;

		public Texture SmoothnessTexture { set { smoothnessTexture = value; } get { return smoothnessTexture; } } [SerializeField] private Texture smoothnessTexture;

		public Texture EmissionTexture { set { emissionTexture = value; } get { return emissionTexture; } } [SerializeField] private Texture emissionTexture;

		public Texture HeightTexture { set { heightTexture = value; } get { return heightTexture; } } [SerializeField] private Texture heightTexture;

		public Texture ReplaceOpacityTexture { set { replaceOpacityTexture = value; } get { return replaceOpacityTexture; } } [SerializeField] private Texture replaceOpacityTexture;

		public float NormalStrength { set { normalStrength = value; } get { return normalStrength; } } [SerializeField] private float normalStrength = 1.0f;

		public float MetallicStrength { set { metallicStrength = value; } get { return metallicStrength; } } [SerializeField] private float metallicStrength = 1.0f;

		public float OcclusionStrength { set { occlusionStrength = value; } get { return occlusionStrength; } } [SerializeField] private float occlusionStrength = 1.0f;

		public float SmoothnessStrength { set { smoothnessStrength = value; } get { return smoothnessStrength; } } [SerializeField] private float smoothnessStrength = 1.0f;

		public float EmissionStrength { set { emissionStrength = value; } get { return emissionStrength; } } [SerializeField] private float emissionStrength = 1.0f;

		public float HeightStrength { set { heightStrength = value; } get { return heightStrength; } } [SerializeField] private float heightStrength = 1.0f;

		public float TilingScale { set { tilingScale = value; } get { return tilingScale; } } [SerializeField] private float tilingScale = 1.0f;

		public float TilingStrength { set { tilingStrength = value; } get { return tilingStrength; } } [SerializeField] private float tilingStrength = 1.0f;

		public float TilingTransition { set { tilingTransition = value; } get { return tilingTransition; } } [SerializeField] private float tilingTransition = 10.0f;

		private static int _CwPaintTint                 = Shader.PropertyToID("_CwPaintTint");
		private static int _CwPaintMatrix               = Shader.PropertyToID("_CwPaintMatrix");
		private static int _CwPaintShape                = Shader.PropertyToID("_CwPaintShape");
		private static int _CwPaintTexture              = Shader.PropertyToID("_CwPaintTexture");
		private static int _CwPaintShapeChannel         = Shader.PropertyToID("_CwPaintShapeChannel");
		private static int _CwPaintOpacity              = Shader.PropertyToID("_CwPaintOpacity");
		private static int _CwPaintOrigin               = Shader.PropertyToID("_CwPaintOrigin");
		private static int _CwPaintDirection            = Shader.PropertyToID("_CwPaintDirection");
		private static int _CwPaintPerspective          = Shader.PropertyToID("_CwPaintPerspective");
		private static int _CwTileTexture               = Shader.PropertyToID("_CwTileTexture");
		private static int _CwTileScale                 = Shader.PropertyToID("_CwTileScale");
		private static int _CwTileStrength              = Shader.PropertyToID("_CwTileStrength");
		private static int _CwTileTransition            = Shader.PropertyToID("_CwTileTransition");
		private static int _CwSwizzleR                  = Shader.PropertyToID("_CwSwizzleR");
		private static int _CwSwizzleG                  = Shader.PropertyToID("_CwSwizzleG");
		private static int _CwSwizzleB                  = Shader.PropertyToID("_CwSwizzleB");
		private static int _CwSwizzleA                  = Shader.PropertyToID("_CwSwizzleA");
		private static int _CwSwizzleX                  = Shader.PropertyToID("_CwSwizzleX");
		private static int _CwSwizzleY                  = Shader.PropertyToID("_CwSwizzleY");
		private static int _CwSwizzleZ                  = Shader.PropertyToID("_CwSwizzleZ");
		private static int _CwSwizzleW                  = Shader.PropertyToID("_CwSwizzleW");
		private static int _CwDepthTexture              = Shader.PropertyToID("_CwDepthTexture");
		private static int _CwDepthMatrix               = Shader.PropertyToID("_CwDepthMatrix");
		private static int _CwDepthBias                 = Shader.PropertyToID("_CwDepthBias");
		private static int _CwCoord                     = Shader.PropertyToID("_CwCoord");
		private static int _CwTileOpacityTexture        = Shader.PropertyToID("_CwTileOpacityTexture");
		private static int _CwTileOpacityTextureChannel = Shader.PropertyToID("_CwTileOpacityTextureChannel");
		private static int _CwOriginalTexture           = Shader.PropertyToID("_CwOriginalTexture");

		private static Material decalMaterial;

		public override bool NeedsShape
		{
			get
			{
				return true;
			}
		}

		public override Texture GetShape()
		{
			if (CwPaintInEditor.Settings.Wallpaper.Shape != null)
			{
				return CwPaintInEditor.Settings.Wallpaper.Shape.Texture;
			}

			return Texture2D.whiteTexture;
		}

		public override float GetSize()
		{
			return CwPaintInEditor.Settings.Wallpaper.Size;
		}

		public override float GetAngle()
		{
			return CwPaintInEditor.Settings.Wallpaper.Angle;
		}

		public override void DrawSettings(CwPaintInEditor.SettingsData settings)
		{
			settings.Wallpaper.Tint    = EditorGUILayout.ColorField("Tint", settings.Wallpaper.Tint);
			settings.Wallpaper.Opacity = EditorGUILayout.Slider("Opacity", settings.Wallpaper.Opacity, 0.0f, 1.0f);
			settings.Wallpaper.Size    = EditorGUILayout.Slider("Size", settings.Wallpaper.Size, 1.0f, 1000.0f);
			settings.Wallpaper.Angle   = EditorGUILayout.Slider("Angle", settings.Wallpaper.Angle, -360.0f, 360.0f);
			settings.Wallpaper.Spacing = EditorGUILayout.IntSlider("Spacing", settings.Wallpaper.Spacing, 1, 500);
			settings.Wallpaper.Tiling  = EditorGUILayout.FloatField("Tiling", settings.Wallpaper.Tiling);

			CwEditor.Separator();

			settings.Wallpaper.JitterPosition   = EditorGUILayout.Slider("Jitter Position"  , settings.Wallpaper.JitterPosition  , 0.0f, 1000.0f);
			settings.Wallpaper.JitterSize       = EditorGUILayout.Slider("Jitter Size"      , settings.Wallpaper.JitterSize      , 0.0f, 1000.0f);
			settings.Wallpaper.JitterAngle      = EditorGUILayout.Slider("Jitter Angle"     , settings.Wallpaper.JitterAngle     , 0.0f,  360.0f);
			settings.Wallpaper.JitterOpacity    = EditorGUILayout.Slider("Jitter Opacity"   , settings.Wallpaper.JitterOpacity   , 0.0f,    1.0f);
			settings.Wallpaper.JitterHue        = EditorGUILayout.Slider("Jitter Hue"       , settings.Wallpaper.JitterHue       , 0.0f,    1.0f);
			settings.Wallpaper.JitterSaturation = EditorGUILayout.Slider("Jitter Saturation", settings.Wallpaper.JitterSaturation, 0.0f,    1.0f);
			settings.Wallpaper.JitterLightness  = EditorGUILayout.Slider("Jitter Lightness" , settings.Wallpaper.JitterLightness , 0.0f,    1.0f);

			CwEditor.Separator();

			settings.Wallpaper.WriteAlbedo     = EditorGUILayout.Toggle("Write Albedo"    , settings.Wallpaper.WriteAlbedo    );
			settings.Wallpaper.WriteOpacity    = EditorGUILayout.Toggle("Write Opacity"   , settings.Wallpaper.WriteOpacity   );
			settings.Wallpaper.WriteNormal     = EditorGUILayout.Toggle("Write Normal"    , settings.Wallpaper.WriteNormal    );
			settings.Wallpaper.WriteMetallic   = EditorGUILayout.Toggle("Write Metallic"  , settings.Wallpaper.WriteMetallic  );
			settings.Wallpaper.WriteOcclusion  = EditorGUILayout.Toggle("Write Occlusion" , settings.Wallpaper.WriteOcclusion );
			settings.Wallpaper.WriteSmoothness = EditorGUILayout.Toggle("Write Smoothness", settings.Wallpaper.WriteSmoothness);
			settings.Wallpaper.WriteEmission   = EditorGUILayout.Toggle("Write Emission"  , settings.Wallpaper.WriteEmission  );
			settings.Wallpaper.WriteHeight     = EditorGUILayout.Toggle("Write Height"    , settings.Wallpaper.WriteHeight    );
		}

		private void DrawBrushPoint(CwPaintInEditor.SettingsData settings, CwEditorTextureProfile.Subset subset, Vector2 subPoint)
		{
			var point   = subPoint + Random.insideUnitCircle * settings.Wallpaper.JitterPosition;
			var angle   = Random.Range(-1.0f, 1.0f) * settings.Wallpaper.JitterAngle + settings.Wallpaper.Angle;
			var opacity = Mathf.Clamp01(Random.Range(-1.0f, 1.0f) * settings.Wallpaper.JitterOpacity + settings.Wallpaper.Opacity);
			var deltaH  = Random.Range(-1.0f, 1.0f) * settings.Wallpaper.JitterHue;
			var deltaS  = Random.Range(-1.0f, 1.0f) * settings.Wallpaper.JitterSaturation;
			var deltaV  = Random.Range(-1.0f, 1.0f) * settings.Wallpaper.JitterLightness;
			var matrix  = Matrix4x4.Rotate(Quaternion.Euler(0.0f, 0.0f, angle)) * CwEditorTool.GetMatrix(point);

			decalMaterial.SetFloat(_CwPaintOpacity, opacity);
			decalMaterial.SetMatrix(_CwPaintMatrix, matrix);
			decalMaterial.SetVector(_CwPaintOrigin, CwEditorTool.LastCamera.transform.position);
			decalMaterial.SetVector(_CwPaintDirection, matrix.MultiplyVector(Vector3.forward).normalized);
			decalMaterial.SetFloat(_CwPaintPerspective, CwEditorTool.LastCamera.orthographic ? 0.0f : 1.0f);
			decalMaterial.SetColor(_CwPaintTint, new Color(1.0f, 1.0f, 1.0f, 1.0f));

			switch (subset.Name)
			{
				case "Albedo": if (albedoTexture != null && settings.Wallpaper.WriteAlbedo == true)
				{
					float h, s, v; Color.RGBToHSV(settings.Wallpaper.Tint, out h, out s, out v);

					h = Mathf.Repeat(h + deltaH, 1.0f);
					s = Mathf.Clamp01(s + deltaS);
					v = Mathf.Clamp01(v + deltaV);

					decalMaterial.SetColor(_CwPaintTint, Color.HSVToRGB(h, s, v));
					decalMaterial.SetTexture(_CwTileTexture, albedoTexture);
					CwPaintInEditor.PaintNow(decalMaterial);
				}
				break;

				case "Opacity": if (replaceOpacityTexture != null && settings.Wallpaper.WriteOpacity == true)
				{
					decalMaterial.SetTexture(_CwTileTexture, replaceOpacityTexture);
					CwPaintInEditor.PaintNow(decalMaterial);
				}
				break;

				case "Normal": if (normalTexture != null && settings.Wallpaper.WriteNormal == true)
				{
					decalMaterial.SetTexture(_CwTileTexture, normalTexture);
					CwPaintInEditor.PaintNow(decalMaterial);
				}
				break;

				case "Metallic": if (metallicTexture != null && settings.Wallpaper.WriteMetallic == true)
				{
					decalMaterial.SetTexture(_CwTileTexture, metallicTexture);
					CwPaintInEditor.PaintNow(decalMaterial);
				}
				break;

				case "Occlusion": if (occlusionTexture != null && settings.Wallpaper.WriteOcclusion == true)
				{
					decalMaterial.SetTexture(_CwTileTexture, occlusionTexture);
					CwPaintInEditor.PaintNow(decalMaterial);
				}
				break;

				case "Smoothness": if (smoothnessTexture != null && settings.Wallpaper.WriteSmoothness == true)
				{
					decalMaterial.SetTexture(_CwTileTexture, smoothnessTexture);
					CwPaintInEditor.PaintNow(decalMaterial);
				}
				break;

				case "Emission": if (emissionTexture != null && settings.Wallpaper.WriteEmission == true)
				{
					decalMaterial.SetTexture(_CwTileTexture, emissionTexture);
					CwPaintInEditor.PaintNow(decalMaterial);
				}
				break;

				case "Height": if (heightTexture != null && settings.Wallpaper.WriteHeight == true)
				{
					decalMaterial.SetTexture(_CwTileTexture, heightTexture);
					CwPaintInEditor.PaintNow(decalMaterial);
				}
				break;
			}
		}

		public override void DrawBrush(CwPaintInEditor.SettingsData settings, CwEditorPaintable paintable, CwEditorTextureProfile.Subset subset, double oldDistance, Vector2 oldPoint, Vector2 newPoint, bool down)
		{
			if (decalMaterial == null) decalMaterial = CwHelper.CreateTempMaterial("CwDecal", "Hidden/PaintInEditor/CwDecal");

			if (down == true)
			{
				CwPaintInEditor.StoreUndoState();
			}

			if (blend == BlendType.Replace)
			{
				if (subset.Name == "Normal")
				{
					decalMaterial.shaderKeywords = new string[] { "CW_REPLACE", "CW_NORMAL", "CW_WALLPAPER" };
				}
				else
				{
					decalMaterial.shaderKeywords = new string[] { "CW_REPLACE", "CW_REPLACE", "CW_WALLPAPER" };
				}
			}

			if (blend == BlendType.Combine)
			{
				if (subset.Name == "Normal")
				{
					decalMaterial.shaderKeywords = new string[] { "CW_COMBINE", "CW_NORMAL", "CW_WALLPAPER" };
				}
				else
				{
					decalMaterial.shaderKeywords = new string[] { "CW_COMBINE", "CW_REPLACE", "CW_WALLPAPER" };
				}
			}

			if (blend == BlendType.Erase)
			{
				decalMaterial.SetTexture(_CwOriginalTexture, paintable.Original);

				if (subset.Name == "Normal")
				{
					decalMaterial.shaderKeywords = new string[] { "CW_ERASE", "CW_NORMAL", "CW_WALLPAPER" };
				}
				else
				{
					decalMaterial.shaderKeywords = new string[] { "CW_ERASE", "CW_RGBA", "CW_WALLPAPER" };
				}
			}

			decalMaterial.SetTexture(_CwPaintTexture, Texture2D.whiteTexture);
			decalMaterial.SetTexture(_CwPaintShape, settings.Wallpaper.Shape != null && settings.Wallpaper.Shape.Texture != null ? settings.Wallpaper.Shape.Texture : Texture2D.whiteTexture);
			decalMaterial.SetVector(_CwPaintShapeChannel, CwCommon.GetTextureChannel(settings.Wallpaper.Shape));

			decalMaterial.SetFloat(_CwTileScale, tilingScale * settings.Wallpaper.Tiling);
			decalMaterial.SetFloat(_CwTileStrength, tilingStrength);
			decalMaterial.SetFloat(_CwTileTransition, tilingTransition);

			decalMaterial.SetVector(_CwSwizzleR, subset.SwizzleR);
			decalMaterial.SetVector(_CwSwizzleG, subset.SwizzleG);
			decalMaterial.SetVector(_CwSwizzleB, subset.SwizzleB);
			decalMaterial.SetVector(_CwSwizzleA, subset.SwizzleA);
			decalMaterial.SetVector(_CwSwizzleX, subset.SwizzleX);
			decalMaterial.SetVector(_CwSwizzleY, subset.SwizzleY);
			decalMaterial.SetVector(_CwSwizzleZ, subset.SwizzleZ);
			decalMaterial.SetVector(_CwSwizzleW, subset.SwizzleW);

			decalMaterial.SetVector(_CwCoord, CwCommon.GetChannelVector(paintable.SlotChannel));
			decalMaterial.SetTexture(_CwDepthTexture, CwEditorTool.LastDepthTexture);
			decalMaterial.SetMatrix(_CwDepthMatrix, CwEditorTool.LastDepthMatrix);
			decalMaterial.SetFloat(_CwDepthBias, settings.DepthBias);

			if (opacityTexture != null)
			{
				decalMaterial.SetTexture(_CwTileOpacityTexture, opacityTexture);
				decalMaterial.SetVector(_CwTileOpacityTextureChannel, CwCommon.GetTextureChannel(opacityTexture));
			}
			else
			{
				decalMaterial.SetTexture(_CwTileOpacityTexture, Texture2D.whiteTexture);
				decalMaterial.SetVector(_CwTileOpacityTextureChannel, new Vector4(1.0f, 0.0f, 0.0f, 0.0f));
			}

			if (down == true)
			{
				DrawBrushPoint(settings, subset, oldPoint);
			}

			if (Vector2.Distance(oldPoint, newPoint) > 0.0f)
			{
				var remaining = (float)(oldDistance % settings.Wallpaper.Spacing);
				var delta     = (newPoint - oldPoint).normalized;

				oldPoint -= delta * remaining;

				remaining = Vector2.Distance(oldPoint, newPoint);

				if (remaining >= settings.Wallpaper.Spacing)
				{
					while (remaining >= settings.Wallpaper.Spacing)
					{
						remaining -= settings.Wallpaper.Spacing;

						oldPoint += delta * settings.Wallpaper.Spacing;

						DrawBrushPoint(settings, subset, oldPoint);
					}
				}
			}
		}
	}
}

#if UNITY_EDITOR
namespace PaintInEditor
{
	using CW.Common;
	using UnityEditor;
	using TARGET = CwEditorBrush_Wallpaper;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(TARGET))]
	public class CwEditorBrush_Wallpaper_Editor : CwEditorBrush_Editor
	{
		private RenderTexture thumb;

		public override bool HasPreviewGUI()
		{
			return true;
		}

		protected override void OnInspector()
		{
			TARGET tgt; TARGET[] tgts; GetTargets(out tgt, out tgts);

			EditorGUILayout.BeginHorizontal();
				Draw("icon");
				if (GUILayout.Button("Regenerate", GUILayout.Width(90)) == true)
				{
					RegenerateThumb(tgt, 512);

					SaveIcon(tgt, thumb);
				}
			EditorGUILayout.EndHorizontal();
			Draw("blend");

			Separator();

			Draw("albedoTexture"); TryDrawWarnings(tgt.AlbedoTexture, false, true, true);
			Draw("opacityTexture"); TryDrawWarnings(tgt.OpacityTexture, false, true, false);
			Draw("normalTexture"); TryDrawWarnings(tgt.NormalTexture, true, true, false);
			Draw("metallicTexture"); TryDrawWarnings(tgt.MetallicTexture, false, true, false);
			Draw("occlusionTexture"); TryDrawWarnings(tgt.OcclusionTexture, false, true, false);
			Draw("smoothnessTexture"); TryDrawWarnings(tgt.SmoothnessTexture, false, true, false);
			Draw("emissionTexture"); TryDrawWarnings(tgt.EmissionTexture, false, true, false);
			Draw("heightTexture"); TryDrawWarnings(tgt.HeightTexture, false, true, false);
			Draw("replaceOpacityTexture"); TryDrawWarnings(tgt.ReplaceOpacityTexture, false, true, false);

			Separator();

			Draw("normalStrength");
			Draw("metallicStrength");
			Draw("occlusionStrength");
			Draw("smoothnessStrength");
			Draw("emissionStrength");
			Draw("heightStrength");

			Separator();

			Draw("tilingScale");
			Draw("tilingStrength");
			Draw("tilingTransition");
		}

		protected virtual void OnDestroy()
		{
			if (thumb != null)
			{
				thumb.Release();

				DestroyImmediate(thumb);

				thumb = null;
			}
		}

		public override void OnPreviewGUI(Rect r, GUIStyle background)
		{
			var tgt  = (CwEditorBrush_Wallpaper)target;
			var size = (int)Mathf.Min(r.width, r.height);

			RegenerateThumb(tgt, size);

			EditorGUI.DrawTextureTransparent(r, thumb, ScaleMode.ScaleToFit, 1.0f, 0);
		}

		[MenuItem("Assets/Create/CW/Paint in Editor/Brush (Wallpaper)")]
		private static void CreateAsset_Paint()
		{
			var asset = CreateAsset_AndBuildTempTextures<CwEditorBrush_Wallpaper>(asset =>
				{
					asset.AlbedoTexture     = tempTextures.Find(t => t.name.EndsWith("_Albedo", System.StringComparison.InvariantCultureIgnoreCase));
					asset.OpacityTexture    = tempTextures.Find(t => t.name.EndsWith("_Opacity", System.StringComparison.InvariantCultureIgnoreCase));
					asset.NormalTexture     = tempTextures.Find(t => t.name.EndsWith("_Normal", System.StringComparison.InvariantCultureIgnoreCase));
					asset.MetallicTexture   = tempTextures.Find(t => t.name.EndsWith("_Metallic", System.StringComparison.InvariantCultureIgnoreCase));
					asset.OcclusionTexture  = tempTextures.Find(t => t.name.EndsWith("_Occlusion", System.StringComparison.InvariantCultureIgnoreCase));
					asset.SmoothnessTexture = tempTextures.Find(t => t.name.EndsWith("_Smoothness", System.StringComparison.InvariantCultureIgnoreCase));
					asset.EmissionTexture   = tempTextures.Find(t => t.name.EndsWith("_Emission", System.StringComparison.InvariantCultureIgnoreCase));
					asset.HeightTexture     = tempTextures.Find(t => t.name.EndsWith("_Height", System.StringComparison.InvariantCultureIgnoreCase));

					if (asset.AlbedoTexture != null && asset.OpacityTexture != null)
					{
						asset.ReplaceOpacityTexture = asset.OpacityTexture;
					}
				});
		}

		private void RegenerateThumb(CwEditorBrush_Wallpaper tgt, int size)
		{
			thumb = CwEditorThumb.Generate(size, CwCommon.GetSphereMesh(), new Vector2(4.0f, 2.0f),
				tgt.AlbedoTexture, tgt.OpacityTexture,
				tgt.NormalTexture, tgt.NormalStrength,
				tgt.MetallicTexture, tgt.MetallicTexture ? tgt.MetallicStrength : 0.0f,
				tgt.SmoothnessTexture, tgt.SmoothnessTexture ? tgt.SmoothnessStrength : 0.0f,
				tgt.OcclusionTexture, tgt.OcclusionTexture ? tgt.OcclusionStrength : 0.0f,
				tgt.EmissionTexture, tgt.EmissionTexture ? tgt.EmissionStrength : 0.0f,
				thumb);
		}
	}
}
#endif