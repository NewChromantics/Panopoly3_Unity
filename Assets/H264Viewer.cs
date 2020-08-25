using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class H264Viewer : MonoBehaviour
{
	PopH264.Decoder Decoder;
	public PopH264.DecoderMode DecoderMode = PopH264.DecoderMode.MagicLeap_NvidiaHardware;
	public bool ThreadedDecoding = true;
	int FrameCounter = 0;
	List<Texture2D> FramePlaneTextures;
	List<Pop.PixelFormat> FramePlaneFormats;
	public List<string> TextureUniformNames;

	PopCapFrameMeta LastMeta;



	[System.Serializable]
	public class H264EncoderParams_Meta
	{
		public int AverageKbps;
		public int BSlicedThreads;
		public bool CpuOptimisations;
		public bool Deterministic;
		public int EncoderThreads;
		public int LookaheadThreads;
		public int MaxFrameBuffers;
		public int MaxKbps;
		public int MaxSliceBytes;
		public bool MaximisePowerEfficiency;
		public int ProfileLevel;
		public float Quality;
		public bool Realtime;
		public bool VerboseDebug;
	};

	[System.Serializable]
	public class YuvEncoderParams_Meta
	{
		public int ChromaRangeCount;
		public int DepthMaxMm;
		public int DepthMinMm;
		public bool PingPongLuma;
	};


	[System.Serializable]
	public class FrameMetaMeta
	{
		public H264EncoderParams_Meta EncoderParams;
		public YuvEncoderParams_Meta YuvEncodeParams;
		public int FrameTimeMs;
		public int Time;
		public int YuvEncode_StartTime;
		public int YuvEncode_DurationMs;

		//	data sent to encoder we dont care about
		public int ChromaUSize;
		public int ChromaVSize;
		public string Format;
		public int Width;
		public int Height;
		public bool Keyframe;
		public int LumaSize;
	};

	[System.Serializable]
	public class PopCapFrameMeta
	{
		public int OutputQueueCount;    //	at the time, number of h264 packets that were queued
		public string CameraName;
		public int DataSize;        //	this is the h264 packet size
		public int OutputTimeMs;        //	time the packet was sent to network/file
		public FrameMetaMeta Meta;
	};



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

		Debug.Log("New frame " + NewFrameNumber.Value);

		var Meta = GetMeta(NewFrameNumber.Value);

		//	gr this needs to sync with frame output
		UpdateMaterial(Meta.Meta.YuvEncodeParams, FramePlaneTextures, TextureUniformNames);
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
