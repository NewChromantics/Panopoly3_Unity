Shader "Panopoly/Yuv_8_8_8_RangesToDepth"
{
	Properties
	{
		[MainTexture]LumaPlane("Texture", 2D) = "white" {}
		ChromaUPlane("Texture", 2D) = "white" {}
		ChromaVPlane("Texture", 2D) = "white" {}

		[Header(Encoding Params from PopCap)]Encoded_DepthMinMetres("Encoded_DepthMinMetres",Range(0,30)) = 0
		Encoded_DepthMaxMetres("Encoded_DepthMaxMetres",Range(0,30)) = 5
		[IntRange]Encoded_DepthRanges("Encoded_DepthRanges",Range(1,128)) = 1
		[Toggle]Encoded_LumaPingPong("Encoded_LumaPingPong",Range(0,1)) = 1
	}
	
		SubShader
		{
			Tags { "RenderType" = "Opaque" }
			LOD 100

			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				#include "YuvToDepth.cginc"
				#include "UnityCG.cginc"

				struct appdata
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f
				{
					float2 uv : TEXCOORD0;
					float4 vertex : SV_POSITION;
				};

				sampler2D LumaPlane;
				float4 LumaPlane_ST;
				sampler2D ChromaUPlane;
				sampler2D ChromaVPlane;
				int Encoded_DepthRanges;
				float Encoded_DepthMinMetres;
				float Encoded_DepthMaxMetres;
				bool Encoded_LumaPingPong;

				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = TRANSFORM_TEX(v.uv, LumaPlane);
					return o;
				}

				float3 NormalToRedGreen(float Normal)
				{
					if (Normal < 0.0)
					{
						return float3(0, 1, 1);
					}
					if (Normal < 0.5)
					{
						Normal = Normal / 0.5;
						return float3(1, Normal, 0);
					}
					else if (Normal <= 1)
					{
						Normal = (Normal - 0.5) / 0.5;
						return float3(1 - Normal, 1, 0);
					}

					//	>1
					return float3(0, 0, 1);
				}

				fixed4 frag(v2f i) : SV_Target
				{
					PopYuvEncodingParams Params;
					Params.DepthRanges = Encoded_DepthRanges;
					Params.DepthMinMetres = Encoded_DepthMinMetres;
					Params.DepthMaxMetres = Encoded_DepthMaxMetres;
					Params.PingPongLuma = Encoded_LumaPingPong;

					float Luma = tex2D(LumaPlane, i.uv).x;
					float ChromaU = tex2D(ChromaUPlane, i.uv).x;
					float ChromaV = tex2D(ChromaVPlane, i.uv).x;

					float Depth = GetLocalDepth(Luma, ChromaU, ChromaV, Params);
					float3 Rgb = NormalToRedGreen(Depth);
					return float4(Rgb, 1.0);
				}
				ENDCG
			}
		}
}
