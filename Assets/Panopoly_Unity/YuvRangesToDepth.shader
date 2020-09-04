Shader "Panopoly/YuvRangesToDepth"
{
	Properties
	{
		_Angle ("Angle", Range(-5.0,  5.0)) = 0.0

		[MainTexture]LumaPlane("LumaPlane", 2D) = "white" {}
		Plane2("Plane2", 2D) = "white" {}
		Plane3("Plane3", 2D) = "white" {}
		[IntRange]PlaneCount("PlaneCount",Range(0,3)) = 3

		DepthTex("Depth Tex", 2D) = "white" {}

		[Header(Encoding Params from PopCap)]Encoded_DepthMinMetres("Encoded_DepthMinMetres",Range(0,30)) = 0
		Encoded_DepthMaxMetres("Encoded_DepthMaxMetres",Range(0,30)) = 5
		[IntRange]Encoded_ChromaRangeCount("Encoded_ChromaRangeCount",Range(1,128)) = 1
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


				float _Angle;
				sampler2D LumaPlane;
				float4 LumaPlane_ST;
				sampler2D Plane2;
				sampler2D Plane3;
				sampler2D DepthTex;
				int PlaneCount;
				#define ChromaUPlane	Plane2
				#define ChromaVPlane	Plane3
				#define ChromaUVPlane	Plane2

				int Encoded_ChromaRangeCount;
				float Encoded_DepthMinMetres;
				float Encoded_DepthMaxMetres;
				bool Encoded_LumaPingPong;

				v2f vert(appdata v)
				{
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);

					// Pivot
	                float2 pivot = float2(0.5, 0.5);
	                // Rotation Matrix
	                float cosAngle = cos(_Angle);
	                float sinAngle = sin(_Angle);
	                float2x2 rot = float2x2(cosAngle, -sinAngle, sinAngle, cosAngle);
	 
	                // Rotation consedering pivot
	                float2 uv = TRANSFORM_TEX(v.uv, LumaPlane) - pivot;
	                o.uv = mul(rot, uv);
	                o.uv += pivot;
					
					//o.uv = TRANSFORM_TEX(v.uv, LumaPlane);
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
	
				float GetLuma(float2 uv)
				{
					return tex2D(LumaPlane, uv).x;
				}			

				float2 GetChromaUv(float2 uv)
				{
					if ( PlaneCount == 2 )
					{
						return tex2D(ChromaUPlane, uv).xy;
					}
					
					float ChromaU = tex2D(ChromaUPlane, uv).x;
					float ChromaV = tex2D(ChromaVPlane, uv).x;
					return float2(ChromaU,ChromaV);
				}
				
				fixed4 frag(v2f i) : SV_Target
				{
					PopYuvEncodingParams Params;
					Params.ChromaRangeCount = Encoded_ChromaRangeCount;
					Params.DepthMinMetres = Encoded_DepthMinMetres;
					Params.DepthMaxMetres = Encoded_DepthMaxMetres;
					Params.PingPongLuma = Encoded_LumaPingPong;

					float Luma = GetLuma(i.uv);
					float2 ChromaUV = GetChromaUv(i.uv);

					float Depth = GetLocalDepth(Luma, ChromaUV.x, ChromaUV.y, Params);
					float3 Rgb = NormalToRedGreen(Depth);
					
					return float4(Rgb, 1.0);
				}
				ENDCG
			}
		}
}
