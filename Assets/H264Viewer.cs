using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PopCap;


[System.Serializable]
public class UnityEvent_TextureAndTime : UnityEngine.Events.UnityEvent<Texture,int> { }

public class H264Viewer : MonoBehaviour
{
	PopH264.Decoder Decoder;
	public PopH264.DecoderMode DecoderMode = PopH264.DecoderMode.MagicLeap_NvidiaHardware;
	public bool ThreadedDecoding = true;
	int FrameCounter = 0;
	List<Texture2D> FramePlaneTextures;
	List<Pop.PixelFormat> FramePlaneFormats;
	public List<string> TextureUniformNames;
	public string StreamFilter;

	[Header("Should we auto create this if blit event/material is set?")]
	public RenderTexture BlitTarget;
	public Material BlitMaterial;
	public UnityEvent_TextureAndTime OnBlit;

	[SerializeField] private bool VerboseDebug = false;
	
	
	PopCapFrameMeta LastMeta;
	PopCapFrameMeta LastStreamMeta;


	//	todo: keep meta associated with frame number here
	PopCapFrameMeta GetMeta(int FrameNumber)
	{
		return LastStreamMeta;
	}

	bool IsLastMetaForThisStream()
	{
		if (string.IsNullOrEmpty(StreamFilter))
			return true;
		if (LastMeta == null)
			return true;
		if (!LastMeta.Stream.Contains(StreamFilter))
			return false;

		return true;
	}

	public void OnMeta(string MetaJson)
	{
		//	todo: store meta with frame counter
		//	this.LastMeta = Meta
		//	in decode;
		//	this.FrameMeta[FrameCounter] = LastMeta
		var NewMeta = JsonUtility.FromJson<PopCapFrameMeta>(MetaJson);
		LastMeta = NewMeta;

		//	apply filter
		if (IsLastMetaForThisStream())
			LastStreamMeta = LastMeta;
	}

	public void DecodeH264Data(byte[] Data)
	{
		//	skip data which is destined for another stream
		if (!IsLastMetaForThisStream())
		{
			Debug.Log("skipping frame; is for stream " + this.LastMeta.Stream);
			return;
		}

		if (Decoder == null)
			Decoder = new PopH264.Decoder(DecoderMode, ThreadedDecoding);

		Decoder.PushFrameData(Data, FrameCounter++);
		if(VerboseDebug)
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

		if(VerboseDebug)
			Debug.Log("New frame " + NewFrameNumber.Value);

		var Meta = GetMeta(NewFrameNumber.Value);

		//	gr this needs to sync with frame output
		//	maybe this component should just blit, then send complete texture with timecode to something else
		var mr = GetComponent<MeshRenderer>();
		if ( mr != null )
			UpdateMaterial( mr.sharedMaterial, Meta.YuvEncodeParams, FramePlaneTextures, TextureUniformNames);
		UpdateBlit(NewFrameNumber.Value,Meta.YuvEncodeParams, FramePlaneTextures, TextureUniformNames);
	}

	void UpdateBlit(int FrameTime,YuvEncoderParams_Meta EncoderParams, List<Texture2D> Planes, List<string> PlaneUniforms)
	{
		if (BlitTarget == null || BlitMaterial == null)
			return;

		UpdateMaterial(BlitMaterial, EncoderParams, Planes, PlaneUniforms);
		//	not best appraoch any more! should be deffered
		Graphics.Blit(null, BlitTarget, BlitMaterial);
		OnBlit.Invoke(BlitTarget, FrameTime);
	}


	void UpdateMaterial(Material material,YuvEncoderParams_Meta EncoderParams, List<Texture2D> Planes, List<string> PlaneUniforms)
	{
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
