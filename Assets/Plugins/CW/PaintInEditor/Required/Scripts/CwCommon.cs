using CW.Common;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace PaintInEditor
{
	/// <summary>This class contains some useful methods used by this asset.</summary>
    internal static class CwCommon
    {
		public const string HelpUrlPrefix = "https://carloswilkes.com/Documentation/PaintInEditor#";

		public const string ComponentMenuPrefix = "CW/Paint in Editor/CW ";

		private static Material normalMaterial;

		private static int _CwBuffer = Shader.PropertyToID("_CwBuffer");

		public static bool ContainsIgnoreCase(string a, string b)
		{
			return a.IndexOf(b, System.StringComparison.InvariantCultureIgnoreCase) >= 0;
		}

		public static void BlitNormal(RenderTexture rt, Texture t)
		{
			if (normalMaterial == null) normalMaterial = CwHelper.CreateTempMaterial("CwNormal", "Hidden/PaintInEditor/CwNormal");

			normalMaterial.SetTexture("_CwBuffer", t);

			Graphics.Blit(null, rt, normalMaterial);
		}

        public static RenderTexture GetRenderTexture(RenderTexture other)
		{
			return GetRenderTexture(other.descriptor, other);
		}

		public static RenderTexture GetRenderTexture(RenderTextureDescriptor desc, RenderTexture other)
		{
			var renderTexture = GetRenderTexture(desc);

			renderTexture.filterMode = other.filterMode;
			renderTexture.anisoLevel = other.anisoLevel;
			renderTexture.wrapModeU  = other.wrapModeU;
			renderTexture.wrapModeV  = other.wrapModeV;

			return renderTexture;
		}

		public static RenderTexture GetRenderTexture(RenderTextureDescriptor desc)
		{
			return GetRenderTexture(desc, QualitySettings.activeColorSpace == ColorSpace.Gamma);
		}

		public static RenderTexture GetRenderTexture(RenderTextureDescriptor desc, bool sRGB)
		{
			//desc.sRGB = sRGB;

			return CwRenderTextureManager.GetTemporary(desc, "CwCommon GetRenderTexture");
		}

		public static RenderTexture ReleaseRenderTexture(RenderTexture renderTexture)
		{
			return CwRenderTextureManager.ReleaseTemporary(renderTexture);
		}

		public static Vector4 GetTextureChannel(CwEditorShape s)
		{
			return GetTextureChannel(s != null ? s.Texture : null);
		}

		public static Vector4 GetTextureChannel(Texture t)
		{
			return t != null && GraphicsFormatUtility.HasAlphaChannel(t.graphicsFormat) ? new Vector4(0,0,0,1) : new Vector4(1,0,0,0);
		}

		private static Mesh quadMesh;
		private static bool quadMeshSet;

		public static Mesh GetQuadMesh()
		{
			if (quadMeshSet == false)
			{
				var gameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);

				quadMeshSet = true;
				quadMesh    = gameObject.GetComponent<MeshFilter>().sharedMesh;

				Object.DestroyImmediate(gameObject);
			}

			return quadMesh;
		}

		private static Mesh cubeMesh;
		private static bool cubeMeshSet;

		public static Mesh GetCubeMesh()
		{
			if (cubeMeshSet == false)
			{
				var gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);

				cubeMeshSet = true;
				cubeMesh    = gameObject.GetComponent<MeshFilter>().sharedMesh;

				Object.DestroyImmediate(gameObject);
			}

			return cubeMesh;
		}

		private static Mesh sphereMesh;
		private static bool sphereMeshSet;

		public static Mesh GetSphereMesh()
		{
			if (sphereMeshSet == false)
			{
				var gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);

				sphereMeshSet = true;
				sphereMesh    = gameObject.GetComponent<MeshFilter>().sharedMesh;

				Object.DestroyImmediate(gameObject);
			}

			return sphereMesh;
		}

		public static Vector4 GetChannelVector(int channel)
		{
			var v = Vector4.zero;

			v[channel] = 1.0f;

			return v;
		}
    }
}