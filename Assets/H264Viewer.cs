using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PopCap;


public class H264Viewer : MonoBehaviour
{
	PopH264.Decoder Decoder;
	public PopH264.DecoderMode DecoderMode = PopH264.DecoderMode.MagicLeap_NvidiaHardware;
	public bool ThreadedDecoding = true;
	int FrameCounter = 0;
	List<Texture2D> FramePlaneTextures;
	List<Pop.PixelFormat> FramePlaneFormats;
	public List<string> TextureUniformNames;

	[SerializeField] private bool debugLogging;
	
	
	PopCapFrameMeta LastMeta;

	//	todo: keep meta associated with frame number here
	PopCapFrameMeta GetMeta(int FrameNumber)
	{
		return LastMeta;
	}

	public void OnMeta(string MetaJson)
	{
		//	todo: store meta with frame counter
		//	this.LastMeta = Meta
		//	in decode;
		//	this.FrameMeta[FrameCounter] = LastMeta
		var NewMeta = JsonUtility.FromJson<PopCapFrameMeta>(MetaJson);

		if (NewMeta != null)
			LastMeta = NewMeta;
	}

	public void DecodeH264Data(byte[] Data)
	{
		if (Decoder == null)
			Decoder = new PopH264.Decoder(DecoderMode, ThreadedDecoding);

		Decoder.PushFrameData(Data, FrameCounter++);
		if(debugLogging)
			Debug.Log("Pushed frame " + FrameCounter);
	}

	void Update()
	{
		if (Decoder != null)
			UpdateFrame();
	}

	void UpdateFrame()
	{
		var NewFrameNumber = Decoder.GetNextFrame(ref FramePlaneTextures, ref FramePlaneFormats);
		if (!NewFrameNumber.HasValue)
			return;

		if(debugLogging)
			Debug.Log("New frame " + NewFrameNumber.Value);

		var Meta = GetMeta(NewFrameNumber.Value);

		//	gr this needs to sync with frame output
		UpdateMaterial(Meta.YuvEncodeParams, FramePlaneTextures, TextureUniformNames);
	}

	void UpdateMaterial(YuvEncoderParams_Meta EncoderParams, List<Texture2D> Planes, List<string> PlaneUniforms)
	{
		var material = GetComponent<MeshRenderer>().sharedMaterial;
	
		material.SetFloat("Encoded_DepthMinMetres", EncoderParams.DepthMinMm / 1000);
		material.SetFloat("Encoded_DepthMaxMetres", EncoderParams.DepthMaxMm / 1000);
		material.SetInt("Encoded_ChromaRangeCount", EncoderParams.ChromaRangeCount);
		material.SetInt("Encoded_LumaPingPong", EncoderParams.PingPongLuma ? 1 : 0);
		material.SetInt("PlaneCount", Planes.Count);

		for ( var i=0;	i<Mathf.Min(Planes.Count,PlaneUniforms.Count);	i++ )
		{
			material.SetTexture(PlaneUniforms[i], Planes[i]);
		}
	}
}
