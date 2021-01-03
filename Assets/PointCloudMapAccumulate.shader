Shader "Panopoly/PointCloudMapAccumulate"
{
    Properties
    {
        CloudPositions("CloudPositions", 2D) = "white" {}
		CloudColours("CloudColours", 2D) = "white" {}

        PointCloudMapLastPositions("PointCloudMapLastPositions", 2D) = "black" {}
        PointCloudMapLastColours("PointCloudMapLastColours", 2D) = "black" {}

        WorldBoundsMin("WorldBoundsMin",Vector) = (0,0,0)
        WorldBoundsMax("WorldBoundsMax",Vector) = (1,1,1)
        //[Header("BlockWidth=TextureWidth, BlockHeight=TextureHeight/Depth")]
        //[IntRange]BlockDepth("BlockDepth",Range(1,100) ) = 2

        [Toggle]BlitInitialise("BlitInitialise",Range(0,1)) =0
        [Toggle]DebugBlitPosition("DebugBlitPosition",Range(0,1)) =0
        [Toggle]DebugSphereFilter("DebugSphereFilter",Range(0,1)) =0
        [Toggle]DebugDrawSphere("DebugDrawSphere",Range(0,1)) =0
        DebugSpherePosition("DebugSpherePosition",Vector) = (0,0,0)
        DebugSphereRadius("DebugSphereRadius",Range(0,1)) = 1
        [IntRange]SampleRow("SampleRow",Range(0,1024)) = 0

        InputPositionsRadius("InputPositionsRadius",Range(0,0.1))=0.01
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


            void CloudSampleRow(float3 WorldPos,float2 RayPosUv,out float4 CloudNearestPosition,out float2 CloudNearestUv)
            {
                //  w=valid
                CloudNearestPosition = float4(WorldPos,0);
                CloudNearestUv = RayPosUv;
            }

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
            
            //  gr: make sure these are integers!
            #define CALC_BLOCKDEPTH int( floor( sqrt(MAP_TEXTURE_HEIGHT) ) )
            #define BLOCKWIDTH  (MAP_TEXTURE_WIDTH)
            #define BLOCKDEPTH  (g_BlockDepth) //  gr: ditch the extra param and calculate it as sqrt instead
            #define BLOCKHEIGHT (int(MAP_TEXTURE_HEIGHT / float(BLOCKDEPTH)))

            float3 WorldBoundsMin;
            float3 WorldBoundsMax;

            float DebugBlitPosition;
            #define DEBUG_BLIT_POSITION false//(DebugBlitPosition>0.5f)

            float BlitInitialise;
#define BLIT_INITIALISE (BlitInitialise>0.5f)

            float DebugSphereFilter;
            #define DEBUG_FILTER_SPHERE (DebugSphereFilter>0.5f)
            float DebugDrawSphere;
            #define DEBUG_DRAW_SPHERE (DebugDrawSphere>0.5f)
            #define DebugSphere float4(DebugSpherePosition,DebugSphereRadius)
            float3 DebugSpherePosition;
            float DebugSphereRadius;


#define WRITE_DISTANCE_TO_ALPHA     true


#define CLOUD_SAMPLE_FUNCTION   CloudSampleRow
#define CLOUD_RAYMARCH_SAMPLE_RADIUS    10
//            #include "PanopolyForUnity/PointCloudRenderer/PointCloudRayMarch.cginc"



            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 ClipPosition : SV_POSITION;
                int BlockDepth : TEXCOORD1;
                float3 xyz : TEXCOORD2;
            };


            int3 PointCloudMapUvToXyz(float2 uv,int g_BlockDepth)
            {
                int x = uv.x * BLOCKWIDTH;
                int Row = uv.y * BLOCKHEIGHT * BLOCKDEPTH;
                int y = Row % BLOCKHEIGHT;
                int z = Row / BLOCKHEIGHT;
                return int3(x,y,z);
            }



            v2f vert (appdata v)
            {
                v2f o;
                o.ClipPosition = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.BlockDepth = CALC_BLOCKDEPTH;

                //  gr: -1 so last entry is normalised to 1.0
                int3 Mapxyz = PointCloudMapUvToXyz(o.uv,o.BlockDepth);
                int g_BlockDepth = o.BlockDepth;
                float3 uvw = Mapxyz.xyz / float3(BLOCKWIDTH-1,BLOCKHEIGHT-1,BLOCKDEPTH-1);
                float3 xyz = lerp( WorldBoundsMin, WorldBoundsMax, uvw );
                o.xyz = xyz;

                return o;
            }

#define BIG_DISTANCE    0.1f

            #define INPUT_POSITION_COUNT  90
            float4 InputPositions[INPUT_POSITION_COUNT];
            float InputPositionsRadius;
            float DistanceSquared(float3 a,float3 b)
            {
                float3 Delta = a-b;
                return dot( Delta, Delta );
	        }


float3 NormalToRedGreen(float Normal)
{
	if (Normal < 0.0)
	{
		return float3(1, 0, 1);
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

            float4 GetCameraNearestCloudPosition(float3 RayPosWorld,out float3 Colour)
            {
                float4 Nearest = float4(0,0,0,0);
                float NearestDistanceSq = 999*999;
                float3 NearestColour = float3(0,0,0);

                for ( int i=0;  i<INPUT_POSITION_COUNT; i++ )
                {
                    float4 InputPosition = InputPositions[i];
                    float DistanceSq = DistanceSquared(InputPosition.xyz,RayPosWorld);
                    float Better = ( DistanceSq < NearestDistanceSq ) ? 1 : 0;

                    Nearest = lerp( Nearest, InputPosition, Better );
                    NearestDistanceSq = lerp( NearestDistanceSq, DistanceSq, Better );
                    NearestColour = lerp( NearestColour, InputPosition.xyz, Better );
                }
                //Colour = NormalToRedGreen(sqrt(NearestDistanceSq)/BIG_DISTANCE);
                Colour = NearestColour;
                return Nearest;
            }
    
            float4 GetOutput(float3 xyz,float4 PreviousPosition,v2f Input)
            {
                //  gr: not sure this matters, but use prev pos if both are valid. Shortest distance wins
    #define INVALID_OLD_DIST    99
    #define INVALID_NEW_DIST    99

                if ( BLIT_INITIALISE )
                {
                    if ( WRITE_DISTANCE_TO_ALPHA )
                        return float4(1,0,1,INVALID_OLD_DIST);

                    return float4(0,0,0,INVALID_OLD_DIST);
				}

/*
                if ( DEBUG_FILTER_SPHERE )
                {
                    if ( distance(xyz,DebugSphere.xyz) > DebugSphere.w )
                        return float4(0,0,0,0);
                }
*/
                if ( DEBUG_DRAW_SPHERE )
                {
                    float3 Colour = normalize(DebugSphere.xyz - xyz) + float3(1,1,1) * float3(0.5,0.5,0.5);
                    //float Stripes = 0.1;
                    //Colour = fmod( uvw, Stripes ) / Stripes;
                    //float3 Colour = float3(1,1,1);

                    float Distance = distance(xyz,DebugSphere.xyz);
                    Distance -= DebugSphere.w;
                    if ( Distance > 0.01 )
                        Colour = float3(0,0,0);
                    //return float4( Colour, Distance );
                    //  overwrite previous data
                    if ( Distance < PreviousPosition.w )
                        PreviousPosition = float4( Colour, Distance );
                }

                float3 CloudColour = float3(0,0,1);
                float4 CloudPosition = GetCameraNearestCloudPosition(xyz,CloudColour);

                CloudPosition.w = (CloudPosition.w > 0) ? (distance(xyz,CloudPosition)-InputPositionsRadius) : INVALID_NEW_DIST;

/*
                if ( DEBUG_BLIT_POSITION )
                {
                    CloudColour = float4(uvw,1);
                    CloudPosition = float4(xyz,1);
				}
*/
               
                bool OldValid = PreviousPosition.w < INVALID_OLD_DIST;
                bool NewValid = CloudPosition.w < INVALID_NEW_DIST;

                //  merge with old value
                //  gr: should use w now
                //float OldDist = OldValid ? distance(xyz,PreviousPosition.xyz) : INVALID_OLD_DIST;
                //float NewDist = NewValid ? distance(xyz,CloudPosition.xyz) : INVALID_NEW_DIST;
                float OldDist = OldValid ? PreviousPosition.w : INVALID_OLD_DIST;
                float NewDist = NewValid ? CloudPosition.w : INVALID_NEW_DIST;

                bool UseNew = (NewDist < OldDist) && NewValid;
                CloudPosition = UseNew ? CloudPosition : PreviousPosition;
                CloudColour = UseNew ? CloudColour : PreviousPosition.xyz;

                CloudPosition.xyz = CloudColour;
                {
/*
                    float OutDistance = distance(xyz,CloudPosition.xyz);

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
            }

            float4 frag (v2f Input) : SV_Target
            {
                //  uv -> xyz, can we interp any of these in vertex?
                int3 Mapxyz = PointCloudMapUvToXyz(Input.uv,Input.BlockDepth);

                //  gr: -1 so last entry is normalised to 1.0
                int g_BlockDepth = Input.BlockDepth;
                float3 uvw = Mapxyz.xyz / float3(BLOCKWIDTH-1,BLOCKHEIGHT-1,BLOCKDEPTH-1);
                float3 xyz = lerp( WorldBoundsMin, WorldBoundsMax, uvw );

                //float3 xyz = Input.xyz;

                float4 OldPosition = tex2D( PointCloudMapLastPositions, Input.uv );
                float4 NewOutput = GetOutput( xyz, OldPosition, Input );
                
                return NewOutput;
            }
            ENDCG
        }
    }
}
