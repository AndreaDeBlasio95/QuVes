Shader "Hidden/PaintInEditor/CwDecal"
{
	Properties
	{
	}
	SubShader
	{
		Blend Off
		Cull Off
		ZWrite Off

		Pass
		{
			CGPROGRAM
			#pragma vertex   Vert
			#pragma fragment Frag
			#pragma multi_compile_local CW_WALLPAPER CW_STICKER
			#pragma multi_compile_local CW_REPLACE CW_COMBINE CW_ERASE
			#pragma multi_compile_local CW_RGBA CW_NORMAL

			#include "CwShared.cginc"

			struct a2v
			{
				float4 vertex    : POSITION;
				float3 normal    : NORMAL;
				float3 tangent   : TANGENT;
				float2 texcoord0 : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
				float2 texcoord2 : TEXCOORD2;
				float2 texcoord3 : TEXCOORD3;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				float3 normal    : NORMAL;
				float4 position : TEXCOORD0;
				float2 texcoord : TEXCOORD1;
				float3 tile     : TEXCOORD2;
				float3 weights  : TEXCOORD3;
				float3 dot_rot  : TEXCOORD4;
				float4 vpos     : TEXCOORD5;
			};

			struct f2g
			{
				float4 color : SV_TARGET;
			};

			float4    _CwPaintTint;
			float     _CwPaintOpacity;
			float4x4  _CwPaintMatrix;
			sampler2D _CwPaintShape;
			sampler2D _CwPaintTexture;
			float4    _CwPaintShapeChannel;
			float3    _CwPaintOrigin;
			float3    _CwPaintDirection;
			float     _CwPaintPerspective;

			sampler2D _CwTileOpacityTexture;
			float4    _CwTileOpacityTextureChannel;
			sampler2D _CwTileTexture;
			float     _CwTileScale;
			float     _CwTileStrength;
			float     _CwTileTransition;

			float4    _CwCoord;
			sampler2D _CwOriginalTexture;
			sampler2D _CwBuffer;
			float2    _CwBufferSize;

			float4 _CwSwizzleR;
			float4 _CwSwizzleG;
			float4 _CwSwizzleB;
			float4 _CwSwizzleA;
			float4 _CwSwizzleX;
			float4 _CwSwizzleY;
			float4 _CwSwizzleZ;
			float4 _CwSwizzleW;

			sampler2D_float _CwDepthTexture;
			float4x4        _CwDepthMatrix;
			float           _CwDepthBias;

			float CW_GetDepthMask(float4 vpos)
			{
				vpos /= vpos.w;
				vpos.z = 1.0f - vpos.z;

				float plane = vpos.z + _CwDepthBias;
				float depth = tex2Dlod(_CwDepthTexture, float4(vpos.xy, 0.0f, 0.0f)).r;

				return _CwDepthBias > -1.0f ? plane > depth : 1.0f;
			}

			void Vert(a2v i, out v2f o)
			{
				float2 texcoord     = i.texcoord0 * _CwCoord.x + i.texcoord1 * _CwCoord.y + i.texcoord2 * _CwCoord.z + i.texcoord3 * _CwCoord.w;
				float4 worldPos     = mul(unity_ObjectToWorld, i.vertex);
				float4 paintPos     = mul(_CwPaintMatrix, worldPos);
				float3 worldNormal  = normalize(mul((float3x3)unity_ObjectToWorld, i.normal));
				float3 worldTangent = normalize(mul((float3x3)unity_ObjectToWorld, i.tangent));
				float3 worldOrigin  = mul(_CwPaintMatrix, float4(0.0f, 0.0f, 0.0f, 1.0f)).xyz;
				float3 worldView    = lerp(-_CwPaintDirection.xyz, normalize(_CwPaintOrigin - worldPos.xyz), _CwPaintPerspective);

				o.vertex   = float4(texcoord * 2.0f - 1.0f, 0.5f, 1.0f);
				o.position = paintPos;
				o.normal   = worldNormal;
				o.texcoord = texcoord;

				o.vpos = mul(_CwDepthMatrix, worldPos);

				o.weights = pow(abs(worldNormal), _CwTileTransition);
				o.weights /= o.weights.x + o.weights.y + o.weights.z;

				o.tile = worldPos * _CwTileScale;

				float3 loc_tan = mul((float3x3)_CwPaintMatrix, worldTangent);
				o.dot_rot.x = dot(worldNormal, worldView);
				//o.dot_rot.y = -atan2(loc_tan.y, loc_tan.x);
				o.dot_rot.yz = float2(loc_tan.y, loc_tan.x);

				#if UNITY_UV_STARTS_AT_TOP
					o.vertex.y = -o.vertex.y;
				#endif
			}

			void Frag(v2f i, out f2g o)
			{
				i.position /= i.position.w;
				float  distance = length(i.position.xyz);
				float3 absPos   = abs(i.position.xyz);

				float2 coord  = CW_SnapToPixel(i.texcoord, _CwBufferSize);
				float4 buffer = CW_SampleMip0(_CwBuffer, coord);
				float4 current;

				current.x = dot(buffer, _CwSwizzleX);
				current.y = dot(buffer, _CwSwizzleY);
				current.z = dot(buffer, _CwSwizzleZ);
				current.w = dot(buffer, _CwSwizzleW);

				float4 color    = _CwPaintTint;
				float  strength = _CwPaintOpacity;

				// Tiling color
				#if CW_WALLPAPER
					float2 coordX = i.tile.zy; if (i.normal.x < 0) coordX.x = -coordX.x;
					float2 coordY = i.tile.xz; if (i.normal.y < 0) coordY.x = -coordY.x;
					float2 coordZ = i.tile.xy; if (i.normal.z > 0) coordZ.x = -coordZ.x;

					#if CW_RGBA
						float4 textureX = tex2D(_CwTileTexture, coordX) * i.weights.x;
						float4 textureY = tex2D(_CwTileTexture, coordY) * i.weights.y;
						float4 textureZ = tex2D(_CwTileTexture, coordZ) * i.weights.z;

						color *= lerp(float4(1.0f, 1.0f, 1.0f, 1.0f), textureX + textureY + textureZ, _CwTileStrength);
					#elif CW_NORMAL
						float3 tangentX = UnpackNormal(tex2D(_CwTileTexture, coordX)) * i.weights.x;
						float3 tangentY = UnpackNormal(tex2D(_CwTileTexture, coordY)) * i.weights.y;
						float3 tangentZ = UnpackNormal(tex2D(_CwTileTexture, coordZ)) * i.weights.z;
					
						tangentX = CW_RotateNormal(tangentX, atan2(ddy(coordX), ddx(coordX)));
						tangentY = CW_RotateNormal(tangentY, atan2(ddy(coordY), ddx(coordY)));
						tangentZ = CW_RotateNormal(tangentZ, atan2(ddy(coordZ), ddx(coordZ)));

						color = CW_PackNormal(lerp(float3(0.0f, 0.0f, 1.0f), normalize(tangentX + tangentY + tangentZ), _CwTileStrength));
					#endif
				#endif

				// Tiling opacity
				#if CW_WALLPAPER
					float opacityX = dot(tex2D(_CwTileOpacityTexture, coordX), _CwTileOpacityTextureChannel) * i.weights.x;
					float opacityY = dot(tex2D(_CwTileOpacityTexture, coordY), _CwTileOpacityTextureChannel) * i.weights.y;
					float opacityZ = dot(tex2D(_CwTileOpacityTexture, coordZ), _CwTileOpacityTextureChannel) * i.weights.z;

					strength *= opacityX + opacityY + opacityZ;
				#endif

				// Sticker color
				#if CW_STICKER
					#if CW_RGBA
						color *= tex2D(_CwPaintTexture, i.position.xy * 0.5f + 0.5f);
					#elif CW_NORMAL
						float3 tangent = UnpackNormal(tex2D(_CwPaintTexture, i.position.xy * 0.5f + 0.5f));

						tangent = CW_RotateNormal(tangent, -atan2(i.dot_rot.y, i.dot_rot.z));

						color = CW_PackNormal(tangent);
					#endif
				#endif

				// OOB
				strength *= max(max(absPos.x, absPos.y), absPos.z) <= 1.0f;

				// Shape
				strength *= dot(tex2D(_CwPaintShape, i.position.xy * 0.5f + 0.5f), _CwPaintShapeChannel);

				// Depth
				strength *= CW_GetDepthMask(i.vpos);

				// Normal
				strength *= 1.0f - pow(1.0f - saturate(i.dot_rot.x), 5.0f);

				if (strength <= 0.0f)
				{
					discard;
				}

				// Blend
				float4 final = current;

				#if CW_REPLACE
					#if CW_RGBA
						final = lerp(current, color, strength);
					#elif CW_NORMAL
						float3 curVec = UnpackNormal(current);
						float3 dstVec = UnpackNormal(color);
						curVec = normalize(lerp(curVec, dstVec, strength));
						final = CW_PackNormal(curVec);
					#endif
				#elif CW_COMBINE
					#if CW_RGBA
						final = lerp(current, current * color, strength);
					#elif CW_NORMAL
						float3 curVec = UnpackNormal(current);
						float3 dstVec = UnpackNormal(color);
						dstVec = lerp(float3(0.0f, 0.0f, 1.0f), dstVec, strength);
						curVec = normalize(float3(curVec.xy + dstVec.xy, curVec.z * dstVec.z));
						final = CW_PackNormal(curVec);
					#endif
				#elif CW_ERASE
					float4 original = CW_SampleMip0(_CwOriginalTexture, coord);
					#if CW_RGBA
						final = lerp(buffer, original, strength);
					#elif CW_NORMAL
						float3 curVec = UnpackNormal(buffer);
						float3 dstVec = UnpackNormal(original);
						curVec = normalize(lerp(curVec, dstVec, strength));
						final = CW_PackNormal(curVec);
					#endif
				#endif

				// Swizzle output
				#if CW_REPLACE || CW_COMBINE
					o.color.x = lerp(buffer.x, dot(final, _CwSwizzleR), dot(_CwSwizzleR, _CwSwizzleR));
					o.color.y = lerp(buffer.y, dot(final, _CwSwizzleG), dot(_CwSwizzleG, _CwSwizzleG));
					o.color.z = lerp(buffer.z, dot(final, _CwSwizzleB), dot(_CwSwizzleB, _CwSwizzleB));
					o.color.w = lerp(buffer.w, dot(final, _CwSwizzleA), dot(_CwSwizzleA, _CwSwizzleA));
				#elif CW_ERASE
					o.color.x = lerp(buffer.x, final.x, dot(_CwSwizzleR, _CwSwizzleR));
					o.color.y = lerp(buffer.y, final.y, dot(_CwSwizzleG, _CwSwizzleG));
					o.color.z = lerp(buffer.z, final.z, dot(_CwSwizzleB, _CwSwizzleB));
					o.color.w = lerp(buffer.w, final.w, dot(_CwSwizzleA, _CwSwizzleA));
				#endif
			}
			ENDCG
		}
	}
}
