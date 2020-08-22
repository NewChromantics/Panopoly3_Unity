﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopCapMetaParser : MonoBehaviour
{
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
		public int OutputQueueCount;	//	at the time, number of h264 packets that were queued
		public string CameraName;
		public int DataSize;        //	this is the h264 packet size
		public int OutputTimeMs;		//	time the packet was sent to network/file
		public FrameMetaMeta Meta;
	};


	public void OnMeta(string MetaJson)
	{
		var Meta = JsonUtility.FromJson<PopCapFrameMeta>(MetaJson);

		//	gr this needs to sync with whatever renders the texture
		UpdateMaterial(Meta.Meta.YuvEncodeParams);
	}

	void UpdateMaterial(YuvEncoderParams_Meta EncoderParams)
	{
		var SetMat = GetComponent<PopSetMaterialValue>();
		SetMat.ForEachMaterial(m => UpdateMaterial(m, EncoderParams));
	}

	void UpdateMaterial(Material material,YuvEncoderParams_Meta EncoderParams)
	{
		material.SetFloat("Encoded_DepthMinMetres", EncoderParams.DepthMinMm / 1000);
		material.SetFloat("Encoded_DepthMaxMetres", EncoderParams.DepthMaxMm / 1000);
		material.SetInt("Encoded_ChromaRangeCount", EncoderParams.ChromaRangeCount);
		material.SetInt("Encoded_LumaPingPong", EncoderParams.PingPongLuma ? 1:0);
	}
}
