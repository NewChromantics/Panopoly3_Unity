﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopCapMetaParser : MonoBehaviour
{

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
		
	};

	[System.Serializable]
	public class PopCapFrameMeta
	{
		public int QueuedH264Packets;   //	at the time, number of h264 packets that were queued
		public string CameraName;
		public int DataSize;        //	this is the h264 packet size
		public int OutputTimeMs;        //	time the packet was sent to network/file
		public H264EncoderParams_Meta EncoderParams;
		public YuvEncoderParams_Meta YuvEncodeParams;
		public int FrameTimeMs;
		public int Time;
		public int YuvEncode_StartTime;
		public int YuvEncode_DurationMs;

		//	kinect azure meta 
		public Vector3 Accelerometer;
		public Vector3 Gyro;
		public Matrix4x4 LocalToLensTransform;
		public float MaxFov;
		public float Temperature;
		//	projection matrix values
		public float codx;
		public float cody;
		public float cx;
		public float cy;
		public float fx;
		public float fy;
		public float k1;
		public float k2;
		public float k3;
		public float k4;
		public float k5;
		public float k6;
		public float metric_radius;
		public float p1;
		public float p2;

		//	data sent to encoder we dont care about
		public int ChromaUSize;
		public int ChromaVSize;
		public string Format;   //	original input from camera
		public int Width;
		public int Height;
		public bool Keyframe;
		public int LumaSize;
	};

	public Material YuvToDepthMaterial;

	public void OnMeta(string MetaJson)
	{
		var Meta = JsonUtility.FromJson<PopCapFrameMeta>(MetaJson);

		//	gr this needs to sync with whatever renders the texture
		UpdateMaterial(Meta.YuvEncodeParams);
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
