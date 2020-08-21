using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace Pop
{
	[System.Serializable]
	public class UnityEvent_Texture : UnityEngine.Events.UnityEvent<Texture> { };
	[System.Serializable]
	public class UnityEvent_Textures : UnityEngine.Events.UnityEvent<Texture[]> { };
	[System.Serializable]
	public class UnityEvent_TextureAndNames : UnityEngine.Events.UnityEvent<Texture[], string[]> { };
}


public class H264Viewer : MonoBehaviour
{
	public Pop.UnityEvent_Texture OnLumaChanged;
	public Pop.UnityEvent_Textures OnTexturePlanesChanged;
	public Pop.UnityEvent_TextureAndNames OnTexturePlaneAndNamessChanged;
	PopH264.Decoder Decoder;
	public PopH264.DecoderMode DecoderMode = PopH264.DecoderMode.MagicLeap_NvidiaHardware;
	public bool ThreadedDecoding = true;
	int FrameCounter = 0;
	List<Texture2D> FramePlaneTextures;
	List<Pop.PixelFormat> FramePlaneFormats;
	public List<string> TextureUniformNames;

	public void DecodeH264Data(byte[] Data)
	{
		if (Decoder == null)
			Decoder = new PopH264.Decoder(DecoderMode, ThreadedDecoding);

		Decoder.PushFrameData(Data, FrameCounter++);
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

		//	update material
		OnLumaChanged.Invoke(FramePlaneTextures[0]);
		OnTexturePlanesChanged.Invoke(FramePlaneTextures.ToArray());
		OnTexturePlaneAndNamessChanged.Invoke(FramePlaneTextures.ToArray(),TextureUniformNames.ToArray());
	}
}
