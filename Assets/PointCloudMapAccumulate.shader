Shader "Panopoly/PointCloudMapAccumulate"
{
    Properties
    {
        CloudPositions("CloudPositions", 2D) = "white" {}
		CloudColours("CloudColours", 2D) = "white" {}

        PointCloudMapLastPositions("PointCloudMapLastPositions", 2D) = "black" {}
        PointCloudMapLastColours("PointCloudMapLastColours", 2D) = "black" {}
        [Toggle]WriteColourOutputInsteadOfPosition("WriteColourOutputInsteadOfPosition",Range(0,1))=0 //  else write positions

        WorldBoundsMin("WorldBoundsMin",Vector) = (0,0,0)
        WorldBoundsMax("WorldBoundsMax",Vector) = (1,1,1)
        //[Header("BlockWidth=TextureWidth, BlockHeight=TextureHeight/Depth")]
        [IntRange]BlockDepth("BlockDepth",Range(1,100) ) = 2

        [Toggle]BlitInitialise("BlitInitialise",Range(0,1)) =0
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

            sampler2D PointCloudMapLastPositions;
            float4 PointCloudMapLastPositions_TexelSize;    //  should be same as target
            sampler2D PointCloudMapLastColours;

            //  this/map dimensions
            //  gr: _ScreenParams wasn't giving a good y... zero maybe
            #define MAP_TEXTURE_WIDTH    (PointCloudMapLastPositions_TexelSize.z)
            #define MAP_TEXTURE_HEIGHT   (PointCloudMapLastPositions_TexelSize.w)

            //  to aid visualisation width is bounds width x=x in space
            //  then Y is vertical
            //  in blocks of z
            float BlockDepth;
            //  gr: make sure these are integers!
            #define BLOCKWIDTH  (MAP_TEXTURE_WIDTH)
            #define BLOCKDEPTH  (int(BlockDepth))
            #define BLOCKHEIGHT (int(MAP_TEXTURE_HEIGHT / float(BLOCKDEPTH)))

            float3 WorldBoundsMin;
            float3 WorldBoundsMax;

            float DebugBlitPosition;
            #define DEBUG_BLIT_POSITION false//(DebugBlitPosition>0.5f)

            float BlitInitialise;
#define BLIT_INITIALISE (BlitInitialise>0.5f)

            float DebugSphereFilter;
            #define DEBUG_FILTER_SPHERE (DebugSphereFilter>0.5f)
            #define DebugSphere float4(DebugSpherePosition,DebugSphereRadius)
            float3 DebugSpherePosition;
            float DebugSphereRadius;


            float WriteColourOutputInsteadOfPosition;
#define WRITE_COLOUR    (WriteColourOutputInsteadOfPosition>0.5f)

            #define MAX_DISTANCE    99.0f
#define WRITE_DISTANCE_TO_ALPHA     true

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

            float PositionToDistanceAlpha(float3 MapXyz,float3 Position)
            {
                float Distance = distance(MapXyz,Position);
                Distance /= MAX_DISTANCE;
                Distance = min( 1.0, Distance);
                return 1-Distance;
			}

            float4 GetOutput(int3 Mapxyz,float4 PreviousPosition,float4 PreviousColour)
            {
                if ( BLIT_INITIALISE )
                {
                    if ( WRITE_COLOUR )
                        return float4(1,0,1,0);
                    if ( WRITE_DISTANCE_TO_ALPHA )
                        return float4(-999,-999,-999,MAX_DISTANCE);

                    return float4(0,0,0,0);
				}

                //  gr: -1 so last entry is normalised to 1.0
                float3 uvw = Mapxyz.xyz / float3(BLOCKWIDTH-1,BLOCKHEIGHT-1,BLOCKDEPTH-1);
                float3 xyz = lerp( WorldBoundsMin, WorldBoundsMax, uvw );

                if ( DEBUG_FILTER_SPHERE )
                {
                    if ( distance(xyz,DebugSphere.xyz) > DebugSphere.w )
                        return float4(0,0,0,0);
                }

                float3 CloudColour = float3(0,0,1);
                float4 CloudPosition = GetCameraNearestCloudPosition(xyz,CloudColour);

                CloudPosition.w = (CloudPosition.w > 0) ? distance(xyz,CloudPosition) : 0;

                if ( DEBUG_BLIT_POSITION )
                {
                    CloudColour = float4(uvw,1);
                    CloudPosition = float4(xyz,1);
				}

               
                //  gr: not sure this matters, but use prev pos if both are valid. Shortest distance wins
    #define INVALID_OLD_DIST    999
    #define INVALID_NEW_DIST    998

                bool OldValid = PreviousPosition.w > 0;
                bool NewValid = CloudPosition.w > 0;

                //  merge with old value
                //  gr: should use w now
                //float OldDist = OldValid ? distance(xyz,PreviousPosition.xyz) : INVALID_OLD_DIST;
                //float NewDist = NewValid ? distance(xyz,CloudPosition.xyz) : INVALID_NEW_DIST;
                float OldDist = OldValid ? PreviousPosition.w : INVALID_OLD_DIST;
                float NewDist = NewValid ? CloudPosition.w : INVALID_NEW_DIST;

                bool UseNew = (NewDist < OldDist) && NewValid;
                CloudPosition = UseNew ? CloudPosition : PreviousPosition;
                CloudColour = UseNew ? CloudColour : PreviousColour.xyz;

CloudPosition.xyz = xyz;
                {
                    float OutDistance = distance(xyz,CloudPosition.xyz);
/*
                    if ( WRITE_DISTANCE )
                        CloudPosition.xyz = float3(OutDistance,OutDistance,OutDistance);

                    if ( OutDistance > MAX_DISTANCE )
                        CloudPosition = float4(1,0,1,0);*/
                    //if ( !UseNew )
                     //   CloudPosition = float4(1,0,0,0);
                    /*
                    if ( NewValid )
                        CloudPosition = float4(0,1,0,1);
                    else if ( OldValid )
                        CloudPosition = float4(0,0,1,1);
                    if ( !NewValid && !OldValid )
                        CloudPosition = float4(1,0,0,0);
*/
                }

                return CloudPosition;
				//return WRITE_COLOUR ? float4(CloudColour,CloudPosition.w) : CloudPosition;
            }

            float4 frag (v2f i) : SV_Target
            {
                //  uv -> xyz, can we interp any of these in vertex?
                int x = i.uv.x * BLOCKWIDTH;
                int Row = i.uv.y * BLOCKHEIGHT * BLOCKDEPTH;
                int y = Row % BLOCKHEIGHT;
                int z = Row / BLOCKHEIGHT;

                float4 OldPosition = tex2D( PointCloudMapLastPositions, i.uv );
                float4 OldColour = tex2D( PointCloudMapLastColours, i.uv );
                float4 NewOutput = GetOutput( int3(x,y,z), OldPosition, OldColour );
                
                return NewOutput;
            }
            ENDCG
        }
    }
}
