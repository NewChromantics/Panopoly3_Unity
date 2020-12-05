Shader "Panopoly/PointCloudMapAccumulate"
{
    Properties
    {
        CloudPositions("CloudPositions", 2D) = "white" {}
		CloudColours("CloudColours", 2D) = "white" {}


        WorldBoundsMin("WorldBoundsMin",Vector) = (0,0,0)
        WorldBoundsMax("WorldBoundsMax",Vector) = (1,1,1)
        //[Header("BlockWidth=TextureWidth, BlockHeight=TextureHeight/Depth")]
        [IntRange]BlockDepth("BlockDepth",Range(1,100) ) = 2

        [Toggle]DebugBlitPosition("DebugBlitPosition",Range(0,1)) =0
        [Toggle]DebugSphereFilter("DebugSphereFilter",Range(0,1)) =0
        DebugSpherePosition("DebugSpherePosition",Vector) = (0,0,0)
        DebugSphereRadius("DebugSphereRadius",Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "PanopolyForUnity/PointCloudRenderer/PointCloudRayMarch.cginc"

            //  this/map dimensions
            #define MAP_TEXTURE_WIDTH    (_ScreenParams.x)
            #define MAP_TEXTURE_HEIGHT   (_ScreenParams.y)

            //  to aid visualisation width is bounds width x=x in space
            //  then Y is vertical
            //  in blocks of z
            float BlockDepth;
            #define BLOCKWIDTH  MAP_TEXTURE_WIDTH
            #define BLOCKDEPTH  (BlockDepth)
            #define BLOCKHEIGHT (MAP_TEXTURE_HEIGHT / BLOCKDEPTH)

            float3 WorldBoundsMin;
            float3 WorldBoundsMax;

            float DebugBlitPosition;
            #define DEBUG_BLIT_POSITION (DebugBlitPosition>0.5f)

            float DebugSphereFilter;
            #define DEBUG_FILTER_SPHERE (DebugSphereFilter>0.5f)
            #define DebugSphere float4(DebugSpherePosition,DebugSphereRadius)
            float3 DebugSpherePosition;
            float DebugSphereRadius;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 ClipPosition : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.ClipPosition = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 GetOutput(int3 Mapxyz)
            {
                //  gr: -1 so last entry is normalised to 1.0
                float3 uvw = Mapxyz.xyz / float3(BLOCKWIDTH-1,BLOCKHEIGHT-1,BLOCKDEPTH-1);
                float3 xyz = lerp( WorldBoundsMin, WorldBoundsMax, uvw );

                if ( DEBUG_FILTER_SPHERE )
                {
                    if ( distance(xyz,DebugSphere.xyz) > DebugSphere.w )
                        return float4(0,0,0,0);
                }

                if ( DEBUG_BLIT_POSITION )
                {
                    return float4(xyz,1);
				}

                float3 CloudColour;
                float4 NearCloudPosition = GetCameraNearestCloudPosition(xyz,CloudColour);
				return NearCloudPosition;

                return float4(1,1,1,1);
            }

            float4 frag (v2f i) : SV_Target
            {
                //  uv -> xyz, can we interp any of these in vertex?
                int x = i.uv.x * BLOCKWIDTH;
                int Row = i.uv.y * BLOCKHEIGHT * BLOCKDEPTH;
                int y = Row % BLOCKHEIGHT;
                int z = Row / BLOCKHEIGHT;

                return GetOutput( int3(x,y,z));
            }
            ENDCG
        }
    }
}
