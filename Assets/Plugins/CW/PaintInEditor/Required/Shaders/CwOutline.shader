Shader "Hidden/PaintInEditor/CwOutline"
{
	Properties
	{
	}
	SubShader
	{
		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
				#pragma vertex   Vert
				#pragma fragment Frag

				#include "UnityCG.cginc"

				sampler2D _CwShapeTex;
				float4    _CwShapeChannel;

				struct a2v
				{
					float4 vertex : POSITION;
					float2 uv     : TEXCOORD0;
				};

				struct v2f
				{
					float4 vertex : SV_POSITION;
					float2 uv     : TEXCOORD0;
				};

				void Vert(a2v i, out v2f o)
				{
					o.vertex = UnityObjectToClipPos(i.vertex);
					o.uv     = i.uv;
				}

				float CW_IsInside(float2 uv)
				{
					float2 ab = abs(uv * 2.0f - 1.0f);
					float  am = max(ab.x, ab.y) < 1.0f;

					return dot(tex2D(_CwShapeTex, uv), _CwShapeChannel) > 0.5f ? am : 0.0f;
				}

				float CW_IsOutside(float2 uv, float2 deltaU, float2 deltaV)
				{
					float total = 0.0f;

					total += CW_IsInside(uv + deltaU);
					total += CW_IsInside(uv - deltaU);
					total += CW_IsInside(uv + deltaV);
					total += CW_IsInside(uv - deltaV);

					total += CW_IsInside(uv + deltaU + deltaV);
					total += CW_IsInside(uv - deltaU + deltaV);
					total += CW_IsInside(uv + deltaU - deltaV);
					total += CW_IsInside(uv - deltaU - deltaV);

					return total == 8.0f ? 0.0f : 1.0f;
				}

				float4 Frag(v2f i) : SV_TARGET
				{
					float2 deltaU = ddx(i.uv);
					float2 deltaV = ddy(i.uv);
					float  testA  = CW_IsInside(i.uv);
					float  testB  = CW_IsOutside(i.uv, deltaU, deltaV);
					float  testC  = CW_IsOutside(i.uv, deltaU * 2.0f, deltaV * 2.0f);

					float color = testA * testB;
					float alpha = testA * testC;

					return float4(color, color, color, alpha);
				}
			ENDCG
		}
	}
}
