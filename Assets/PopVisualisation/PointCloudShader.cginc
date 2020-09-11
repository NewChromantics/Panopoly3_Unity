/*
float3 GetTrianglePosition(float TriangleIndex, out float2 ColourUv, out bool Valid)
{
	float MapWidth = 640;// CloudPositions_texelSize.z;
	float u = fmod(TriangleIndex, MapWidth) / MapWidth;
	float v = floor(TriangleIndex / MapWidth) / MapWidth;

	ColourUv = float2(u, 1.0 - v);
	float4 PositionUv = float4(u, v, 0, 0);
	float4 PositionSample = tex2Dlod(CloudPositions, PositionUv);
	Valid = PositionSample.w > 0.5;
	return PositionSample.xyz;
}
*/

void Vertex_uv_TriangleIndex_To_CloudUvs(Texture2D<float4> Positions,SamplerState PositionsSampler,float3 uv_TriangleIndex,float PointSize,out float3 Position,out float2 ColourUv)
{
	float TriangleIndex = uv_TriangleIndex.z;
	float MapWidth = 640;// CloudPositions_texelSize.z;
	float u = fmod(TriangleIndex, MapWidth) / MapWidth;
	float v = floor(TriangleIndex / MapWidth) / MapWidth;

	ColourUv = float2(u, 1.0 - v);
	/*
	//	debugging data from vfx graph... this value is wrong...
	//ColourUv.x = clamp(0,1.0, TriangleIndex / 400000.0);
	if (TriangleIndex < 2)
	{
		ColourUv.x = 1.0;
		ColourUv.y = 0.0;
	}
	else
	{
		ColourUv.x = 0.0;
		ColourUv.y = 1.0;
	}
	*/


	float4 PositionUv = float4(u, v, 0, 0);
	//float4 PositionSample = tex2Dlod(Positions, PositionUv);
	float4 PositionSample = Positions.SampleLevel( PositionsSampler, PositionUv.xy, PositionUv.z);
	

	float3 CameraPosition = PositionSample.xyz;
	//Valid = PositionSample.w > 0.5;

	//	local space offset of the triangle
	float3 VertexPosition = float3(uv_TriangleIndex.xy, 0) * PointSize;
	CameraPosition += VertexPosition;
	
	//return CameraPosition.xyz;
	Position = CameraPosition.xyz;
}


//	gr: shadergraph fails looking for
//		Vertex_uv_TriangleIndex_To_CloudUvs
//	because of missing reference to 
//		Vertex_uv_TriangleIndex_To_CloudUvs_float
void Vertex_uv_TriangleIndex_To_CloudUvs_float(Texture2D<float4> Positions, SamplerState PositionsSampler, float3 uv_TriangleIndex, float PointSize, out float3 Position, out float2 ColourUv)
{
	Vertex_uv_TriangleIndex_To_CloudUvs(Positions, PositionsSampler, uv_TriangleIndex, PointSize, Position, ColourUv);
}

