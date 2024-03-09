Shader "Hidden/PaintInEditor/CwThumb"
{
	Properties
	{
		_CwEnvironmentTex("Environment Tex", CUBE) = "white" {}
		_CwLightDirection("Light Direction", Vector) = (1.0, 1.0, -1.0)

		_CwBaseTex("Base Tex", 2D) = "white" {}
		_CwOpacityTex("Opacity Tex", 2D) = "white" {}
		_CwNormalTex("Normal Tex", 2D) = "bump" {}
		_CwMetallicTex("Metallic Tex", 2D) = "white" {}
		_CwOcclusionTex("Occlusion Tex", 2D) = "white" {}
		_CwSmoothnessTex("Smoothness Tex", 2D) = "white" {}
		_CwEmissionTex("Emission Tex", 2D) = "white" {}
	}
	SubShader
	{
		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex Vert
			#pragma fragment Frag

			#include "UnityCG.cginc"

			sampler2D _CwBaseTex;
			sampler2D _CwOpacityTex;
			sampler2D _CwNormalTex;
			sampler2D _CwMetallicTex;
			sampler2D _CwOcclusionTex;
			sampler2D _CwSmoothnessTex;
			sampler2D _CwEmissionTex;

			float _CwNormalStrength;
			float _CwMetallicStrength;
			float _CwOcclusionStrength;
			float _CwSmoothnessStrength;
			float _CwEmissionStrength;
			
			samplerCUBE _CwEnvironmentTex;
			float3      _CwLightDirection;
			float2      _CwTiling;

			struct appdata
			{
				float4 vertex  : POSITION;
				float2 uv      : TEXCOORD0;
				float3 normal  : NORMAL;
				float4 tangent : TANGENT;
			};

			struct v2f
			{
				float2 uv      : TEXCOORD0;
				float4 vertex  : SV_POSITION;
				float3 normal  : NORMAL;
				float4 tangent : TANGENT;
			};

			float3 UnpackNormal2(float4 color, float bumpStrength)
			{
				float3 normal = UnpackNormal(color);
				normal.xy *= bumpStrength;
				normal.z = sqrt(1.0 - saturate(dot(normal.xy, normal.xy)));
				return normal;
			}

			void Vert(appdata i, out v2f o)
			{
				float3x3 rot = float3x3(-1,0,0, 0,-1,0, 0,0,1);

				o.vertex  = float4(i.vertex.xyz * 1.9f, 1.0f);
				o.normal  = i.normal;
				o.tangent = i.tangent;
				o.uv      = i.uv * _CwTiling;

				o.vertex.xyz = mul(rot, o.vertex.xyz);
				o.normal.xyz = mul(rot, o.normal.xyz);
				o.tangent.xyz = mul(rot, o.tangent.xyz);
			}

			float4 Frag(v2f i) : SV_Target
			{
				// Sample textures
				float3 base       = tex2D(_CwBaseTex, i.uv).rgb;
				float  opacity    = tex2D(_CwOpacityTex, i.uv);
				float3 normal     = UnpackNormal2(tex2D(_CwNormalTex, i.uv), _CwNormalStrength);
				float  occlusion  = lerp(1.0f, tex2D(_CwOcclusionTex, i.uv).r, _CwOcclusionStrength);
				float  smoothness = saturate(tex2D(_CwSmoothnessTex, i.uv).r * _CwSmoothnessStrength);
				float  metallic   = saturate(tex2D(_CwMetallicTex, i.uv).r * _CwMetallicStrength);
				float3 emission   = tex2D(_CwEmissionTex, i.uv).rgb * _CwEmissionStrength;

				float3 binormal = cross(i.normal, i.tangent.xyz) * i.tangent.w;
				float3x3 rotation = float3x3(i.tangent.xyz, binormal, i.normal);
				float3 viewDir = normalize(mul(rotation, float3(0.0f, 0.0f, 1.0f)));
				float3 lightDir = normalize(mul(rotation, _CwLightDirection));

				float3 directLight = max(0.0, dot(normal, -lightDir));
				float3 rimLight = 1.0f - max(0.0, dot(normal, viewDir));
				float3 reflectionDir = normalize(reflect(viewDir, normal));
				float3 specularDir = normalize(reflect(lightDir, normal));
				
				float3 environment = texCUBElod(_CwEnvironmentTex, float4(reflectionDir, (1.0f - smoothness) * 4.0f)).xyz;

				environment = lerp(float3(0.5f, 0.5f, 0.5f), environment, smoothness);

				// Specular
				float csmoo = clamp(smoothness, 0.5, 1.0f);
				float3 specular = pow(saturate(dot(specularDir, viewDir)), pow(csmoo, 9.0f) * 2000.0f) * pow(csmoo, 3.5f);
				float spec_rim = pow(1.0f - smoothness, 2.0f);
				specular *= (1.0f - spec_rim) * 30;
				specular += environment * pow(rimLight, 5.0f) * smoothness * 3;

				float3 light = lerp(directLight, directLight * 0.9f + 0.1f, metallic) + specular;
				float3 diffuseColor = base * lerp(float3(1.0f, 1.0f, 1.0f), environment, metallic) * light;

				return float4(pow(diffuseColor * occlusion + emission, 0.9f) * 2.0f, opacity);
			}
			ENDCG
		}
	}
}