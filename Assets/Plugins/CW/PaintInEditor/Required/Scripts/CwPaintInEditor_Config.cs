using UnityEngine;
using UnityEditor;
using CW.Common;

namespace PaintInEditor
{
	public partial class CwPaintInEditor
	{
		public enum ExportTextureFormat
		{
			PNG,
			TGA
		}

		public enum QuickControl
		{
			None,
			MouseWheel,
			Shift_MouseWheel,
			Control_MouseWheel,
			Alt_MouseWheel,
			Command_MouseWheel,
		}

		[System.Serializable]
		public class SettingsData_Wallpaper
		{
			public CwEditorShape Shape;
			public float         Size            = 100.0f;
			public float         Opacity         = 1.0f;
			public float         Angle           = 0.0f;
			public int           Spacing         = 10;
			public Color         Tint            = Color.white;
			public float         Tiling          = 1.0f;

			public float JitterPosition   = 0.0f;
			public float JitterSize       = 0.0f;
			public float JitterAngle      = 360.0f;
			public float JitterOpacity    = 0.0f;
			public float JitterHue        = 0.0f;
			public float JitterSaturation = 0.0f;
			public float JitterLightness  = 0.0f;

			public bool WriteAlbedo     = true;
			public bool WriteOpacity    = true;
			public bool WriteNormal     = true;
			public bool WriteMetallic   = true;
			public bool WriteOcclusion  = true;
			public bool WriteSmoothness = true;
			public bool WriteEmission   = true;
			public bool WriteHeight     = true;
		}

		[System.Serializable]
		public class SettingsData_Sticker
		{
			public float Size            = 100.0f;
			public float Opacity         = 1.0f;
			public float Angle           = 0.0f;
			public Color Tint            = Color.white;
			public float Pulse           = 0.0f;

			public float JitterPosition   = 0.0f;
			public float JitterSize       = 0.0f;
			public float JitterAngle      = 0.0f;
			public float JitterOpacity    = 0.0f;
			public float JitterHue        = 0.0f;
			public float JitterSaturation = 0.0f;
			public float JitterLightness = 0.0f;

			public bool WriteAlbedo     = true;
			public bool WriteOpacity    = true;
			public bool WriteNormal     = true;
			public bool WriteMetallic   = true;
			public bool WriteOcclusion  = true;
			public bool WriteSmoothness = true;
			public bool WriteEmission   = true;
			public bool WriteHeight     = true;
		}

		[System.Serializable]
		public class SettingsData
		{
			public CwEditorBrush       Brush;
			public int                 IconSize          = 128;
			public bool                Dilate            = true;
			public int                 DilateSteps       = 0;
			public float               DepthBias         = 0.000001f;
			public int                 MinResolution     = 1024;
			public bool                ForceResolution   = false;
			public ExportTextureFormat Format            = ExportTextureFormat.PNG;
			public int                 MaxUndoRedo       = 10;
			public QuickControl        QuickScale        = QuickControl.MouseWheel;
			public QuickControl        QuickRotate       = QuickControl.Control_MouseWheel;
			public bool                AutoDeselectPaint = true;
			public bool                WarnRemoveTexture = true;
			public bool                WarnResizeTexture = true;
			public bool                WarnEraseTexture  = true;

			public SettingsData_Wallpaper Wallpaper = new SettingsData_Wallpaper();
			public SettingsData_Sticker   Sticker   = new SettingsData_Sticker();
		}

		public static SettingsData Settings = new SettingsData();

		private Vector2 configScrollPosition;

		private static bool GetQuickScale(QuickControl control, Event e)
		{
			if (control != QuickControl.None)
			{
				var m = e.modifiers & ~EventModifiers.CapsLock;
				
				if (control == QuickControl.        MouseWheel && m == EventModifiers.None   ) return true;
				if (control == QuickControl.  Shift_MouseWheel && m == EventModifiers.Shift  ) return true;
				if (control == QuickControl.Control_MouseWheel && m == EventModifiers.Control) return true;
				if (control == QuickControl.    Alt_MouseWheel && m == EventModifiers.Alt    ) return true;
				if (control == QuickControl.Command_MouseWheel && m == EventModifiers.Command) return true;
			}

			return false;
		}

		public static bool HandleScrollWheel(Event e, bool increase)
		{
			if (Settings.Brush != null)
			{
				if (GetQuickScale(Settings.QuickScale, e) == true)
				{
					var scale = increase ? 1.1f : 1.0f / 1.1f;

					if (Settings.Brush is CwEditorBrush_Sticker  ) Settings.  Sticker.Size = Mathf.Clamp(Settings.  Sticker.Size * scale, 1.0f, 1000.0f);
					if (Settings.Brush is CwEditorBrush_Wallpaper) Settings.Wallpaper.Size = Mathf.Clamp(Settings.Wallpaper.Size * scale, 1.0f, 1000.0f);

					return true;
				}
				else if (GetQuickScale(Settings.QuickRotate, e) == true)
				{
					var scale = increase ? 5.0f : -5.0f;

					if (Settings.Brush is CwEditorBrush_Sticker  ) Settings.  Sticker.Angle = Mathf.Repeat(Settings.  Sticker.Angle + scale, 360.0f);
					if (Settings.Brush is CwEditorBrush_Wallpaper) Settings.Wallpaper.Angle = Mathf.Repeat(Settings.Wallpaper.Angle + scale, 360.0f);

					return true;
				}
			}

			return false;
		}

		private static void ClearSettings()
		{
			if (EditorPrefs.HasKey("PaintInEditor.Settings") == true)
			{
				EditorPrefs.DeleteKey("PaintInEditor.Settings");

				Settings = new SettingsData();
			}
		}

		private static void SaveSettings()
		{
			EditorPrefs.SetString("PaintInEditor.Settings", EditorJsonUtility.ToJson(Settings));
		}

		private static void LoadSettings()
		{
			if (EditorPrefs.HasKey("PaintInEditor.Settings") == true)
			{
				var json = EditorPrefs.GetString("PaintInEditor.Settings");

				if (string.IsNullOrEmpty(json) == false)
				{
					EditorJsonUtility.FromJsonOverwrite(json, Settings);
				}
			}
		}

		private void DrawConfigTab()
		{
			configScrollPosition = GUILayout.BeginScrollView(configScrollPosition, GUILayout.ExpandHeight(true));
				CwEditor.BeginLabelWidth(100);
					Settings.IconSize        = EditorGUILayout.IntSlider("Icon Size", Settings.IconSize, 32, 256);
					Settings.DepthBias       = EditorGUILayout.Slider(new GUIContent("Depth Bias", "The higher you set this, the deeper paint can penetrate through the edges of objects."), Settings.DepthBias, 0.0f, 1.0f);
					Settings.Dilate          = EditorGUILayout.Toggle(new GUIContent("Dilate", "Automatically extend the edges of textures to remove seams?"), Settings.Dilate);
					Settings.DilateSteps     = EditorGUILayout.IntSlider(new GUIContent("Dilate Steps", "If you see seams at the edge of your painted textures, you can increase this. 0 = Fast Dilate Alternative."), Settings.DilateSteps, 0, 16);
					Settings.MinResolution   = EditorGUILayout.IntField(new GUIContent("Min Resolution", "Painted textures will be initialized with this XY resolution."), Settings.MinResolution);
					Settings.ForceResolution = EditorGUILayout.Toggle(new GUIContent("Force Resolution", "If you paint an existing texture that is higher resolution than MinResolution, force MinResolution to be used?"), Settings.ForceResolution);
					Settings.Format          = (ExportTextureFormat)EditorGUILayout.EnumPopup("Format", Settings.Format);
					Settings.MaxUndoRedo     = EditorGUILayout.IntField("Max Undo/Redo", Settings.MaxUndoRedo);
					Settings.QuickScale      = (QuickControl)EditorGUILayout.EnumPopup("Quick Scale", Settings.QuickScale);
					Settings.QuickRotate     = (QuickControl)EditorGUILayout.EnumPopup("Quick Rotate", Settings.QuickRotate);

					Settings.AutoDeselectPaint = EditorGUILayout.Toggle(new GUIContent("Auto Deselect Paint", "If you open Paint in Editor with objects selected, they will become paintable. Deselect them after this occurs?"), Settings.AutoDeselectPaint);
					Settings.WarnRemoveTexture = EditorGUILayout.Toggle(new GUIContent("Warn Remove Texture", "If you click the X button on a texture on the Objects tab, should you get a warning before it's removed?"), Settings.WarnRemoveTexture);
					Settings.WarnResizeTexture = EditorGUILayout.Toggle(new GUIContent("Warn Resize Texture", "If you click the Resize button on a texture on the Objects tab, should you get a warning before it's resized?"), Settings.WarnResizeTexture);
					Settings.WarnEraseTexture  = EditorGUILayout.Toggle(new GUIContent("Warn Erase Texture", "If you click the Erase button on a texture on the Objects tab, should you get a warning before it's erased?"), Settings.WarnEraseTexture);

					GUILayout.FlexibleSpace();

					if (GUILayout.Button("Clear Cache") == true)
					{
						CwEditorShaderProfile_Editor.ClearCache();
						CwEditorTextureProfile_Editor.ClearCache();
						CwEditorBrush_Editor.ClearCache();
						CwEditorShape_Editor.ClearCache();
					}

					if (GUILayout.Button("Clear Settings") == true)
					{
						if (EditorUtility.DisplayDialog("Are you sure?", "This will reset all editor painting settings to default.", "OK") == true)
						{
							ClearSettings();
						}
					}
				CwEditor.EndLabelWidth();
			GUILayout.EndScrollView();
		}
	}
}