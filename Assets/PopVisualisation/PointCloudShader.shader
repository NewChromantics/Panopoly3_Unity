Shader "Panopoly/PointCloudShader"
{
    Properties
    {
		CloudPositions("CloudPositions", 2D) = "white" {}
		CloudColours("CloudColours", 2D) = "white" {}
		PointSize("PointSize",Range(0.001,1)) = 0.01
		[Toggle]Billboard("Billboard", Range(0,1)) = 1
		[Toggle]DrawInvalidPositions("DrawInvalidPositions",Range(0,1)) = 0
		[Toggle]Debug_InvalidPositions("Debug_InvalidPositions",Range(0,1))= 0

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
		Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float3 uv_TriangleIndex : POSITION;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
				float4 OverrideColour : TEXCOORD1;
            };

			sampler2D CloudPositions;
			sampler2D CloudColours;
			float4 CloudPositions_texelSize;
			float4 CloudColours_texelSize;
			float Billboard;
			float PointSize;
#define ENABLE_BILLBOARD	(Billboard>0.5)
			float DrawInvalidPositions;
			float Debug_InvalidPositions;
#define DRAW_INVALIDPOSITIONS	(DrawInvalidPositions>0.5)
#define DEBUG_INVALIDPOSITIONS	(Debug_InvalidPositions>0.5)

			//float4x4 CameraToWorld;

			float3 GetTrianglePosition(float TriangleIndex,out float2 ColourUv,out bool Valid)
			{
				float MapWidth = 640;// CloudPositions_texelSize.z;
				float u = fmod(TriangleIndex, MapWidth) / MapWidth;
				float v = floor(TriangleIndex / MapWidth) / MapWidth;

				ColourUv = float2(u, 1.0-v);
				float4 PositionUv = float4(u, v,0,0);
				float4 PositionSample = tex2Dlod(CloudPositions, PositionUv);
				Valid = PositionSample.w > 0.5;
				return PositionSample.xyz;
			}

            v2f vert (appdata v)
            {
				float TriangleIndex = v.uv_TriangleIndex.z;
				
				//	position in camera space
				float2 ColourUv;
				bool Valid = true;
				float3 CameraPosition = GetTrianglePosition(TriangleIndex, ColourUv, Valid);

				//	local space offset of the triangle
				float3 VertexPosition = float3(v.uv_TriangleIndex.xy, 0) * PointSize;
				CameraPosition += VertexPosition;

				//	gr: here, do billboarding, and repalce below with UnityWorldToClipPos
				v2f o;
				o.vertex = UnityObjectToClipPos(CameraPosition);
                o.uv = ColourUv;
				o.OverrideColour = float4(0, 0, 0, 0);

				if (!Valid && DEBUG_INVALIDPOSITIONS)
				{
					o.OverrideColour = float4(0, 1, 0, 1);
				}
				else if (!Valid && !DRAW_INVALIDPOSITIONS)
				{
					o.vertex = float4(0, 0, 0, 0);
				}
                return o;
            }

			float4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float4 Colour = tex2D(CloudColours, i.uv);
				Colour = lerp(Colour, i.OverrideColour, i.OverrideColour.w);
                return Colour;
            }
            ENDCG
        }
    }
}
