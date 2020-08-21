struct PopYuvEncodingParams
{
	int DepthRanges;
	float DepthMinMetres;
	float DepthMaxMetres;
	bool PingPongLuma;
};

//	convert YUV sampled values into local/camera depth
//	multiply this, plus camera uv (so u,v,z,1) with a projection matrix to get world space position
float GetLocalDepth(float Luma, float ChromaU, float ChromaV, PopYuvEncodingParams EncodingParams)
{
	//	get large range from chroma map

	//	apply small range from luma

	//	multipy to depth range

	return 0.f;
}

