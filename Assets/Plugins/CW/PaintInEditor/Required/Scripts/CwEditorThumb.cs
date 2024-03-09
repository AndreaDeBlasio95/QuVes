using CW.Common;
using UnityEngine;

namespace PaintInEditor
{
	/// <summary>This class generates a thumbnail preview texture of a PBR material texture set.</summary>
	public static class CwEditorThumb
	{
		private static Material thumbMaterial;

		private static int _CwBaseTex       = Shader.PropertyToID("_CwBaseTex");
		private static int _CwOpacityTex    = Shader.PropertyToID("_CwOpacityTex");
		private static int _CwNormalTex     = Shader.PropertyToID("_CwNormalTex");
		private static int _CwMetallicTex   = Shader.PropertyToID("_CwMetallicTex");
		private static int _CwOcclusionTex  = Shader.PropertyToID("_CwOcclusionTex");
		private static int _CwSmoothnessTex = Shader.PropertyToID("_CwSmoothnessTex");
		private static int _CwEmissionTex   = Shader.PropertyToID("_CwEmissionTex");

		private static int _CwNormalStrength     = Shader.PropertyToID("_CwNormalStrength");
		private static int _CwMetallicStrength   = Shader.PropertyToID("_CwMetallicStrength");
		private static int _CwOcclusionStrength  = Shader.PropertyToID("_CwOcclusionStrength");
		private static int _CwSmoothnessStrength = Shader.PropertyToID("_CwSmoothnessStrength");
		private static int _CwEmissionStrength   = Shader.PropertyToID("_CwEmissionStrength");

		private static int _CwTiling = Shader.PropertyToID("_CwTiling");

		public static RenderTexture Generate(int size, Mesh mesh, Vector2 tiling, Texture baseMap, Texture opacityMap, Texture normalMap, float normalStrength, Texture metallicMap, float metallicStrength, Texture smoothnessMap, float smoothnessStrength, Texture occlusionMap, float occlusionStrength, Texture emissionMap, float emissionStrength, RenderTexture texture = null)
		{
			if (thumbMaterial == null) thumbMaterial = CwHelper.CreateTempMaterial("CwThumb", "Hidden/PaintInEditor/CwThumb");

			if (texture == null)
			{
				texture = new RenderTexture(size, size, 0, RenderTextureFormat.ARGB32, 0);

				texture.Create();
			}
			else if (texture.width != size || texture.height != size)
			{
				texture.Release();

				texture.width = size;
				texture.height = size;

				texture.Create();
			}

			var oldActive = RenderTexture.active;

			thumbMaterial.SetTexture(_CwBaseTex, baseMap);
			thumbMaterial.SetTexture(_CwOpacityTex, opacityMap);
			thumbMaterial.SetTexture(_CwNormalTex, normalMap);
			thumbMaterial.SetTexture(_CwMetallicTex, metallicMap);
			thumbMaterial.SetTexture(_CwOcclusionTex, occlusionMap);
			thumbMaterial.SetTexture(_CwSmoothnessTex, smoothnessMap);
			thumbMaterial.SetTexture(_CwEmissionTex, emissionMap);

			thumbMaterial.SetFloat(_CwNormalStrength, normalStrength);
			thumbMaterial.SetFloat(_CwMetallicStrength, metallicStrength);
			thumbMaterial.SetFloat(_CwOcclusionStrength, occlusionStrength);
			thumbMaterial.SetFloat(_CwSmoothnessStrength, smoothnessStrength);
			thumbMaterial.SetFloat(_CwEmissionStrength, emissionStrength);

			thumbMaterial.SetVector(_CwTiling, tiling);

			RenderTexture.active = texture;

			GL.Clear(true, true, Color.clear);

			if (thumbMaterial.SetPass(0) == true)
			{
				Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
			}

			RenderTexture.active = oldActive;

			return texture;
		}
	}
}