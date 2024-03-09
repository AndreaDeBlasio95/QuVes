Shader "Hidden/PaintInEditor/CwNormal"
{
	Properties
	{
	}
	SubShader
	{
		Cull Off

		Pass
		{
			CGPROGRAM
			#pragma vertex   Vert
			#pragma fragment Frag

			#include "CwShared.cginc"

			sampler2D _CwBuffer;

			struct appdata
			{
				float4 vertex    : POSITION;
				float2 texcoord0 : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv     : TEXCOORD0;
			};

			void Vert (in appdata i, out v2f o)
			{
				o.vertex = UnityObjectToClipPos(i.vertex);
				o.uv     = i.texcoord0;
			}

			fixed4 Frag (v2f i) : SV_Target
			{
				float4 buffer = tex2D(_CwBuffer, i.uv);

				return CW_PackNormal(UnpackNormal(buffer));
			}
			ENDCG
		}
	}
}
