using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointCloudAccumulatorBlitter : MonoBehaviour
{
	public RenderTexture OutputPositions;
	RenderTexture OutputPositionsLast;
	public Material AccumulatorMaterial;
	[Range(1, 1000)]
	public int MaxPositionBlits = 1;
	[Range(0, 640 * 480)]
	public int FirstPositionBlit = 0;

	void OnEnable()
	{
		ResetTexturesAndMaterial();
	}

	void Start()
	{
		OnEnable();
	}

	public void ResetTexturesAndMaterial()
	{
		//	init buffers
		//Graphics.Blit(Texture2D.blackTexture, OutputPositions);
		//Graphics.Blit(Texture2D.blackTexture, OutputColours);
		AccumulatorMaterial.SetFloat("BlitInitialise", 1.0f);
		AccumulatorMaterial.SetFloat("WriteColourOutputInsteadOfPosition", 0.0f);
		Graphics.Blit(null, OutputPositions, AccumulatorMaterial);


		//	make (duplicate) back buffers
		OutputPositionsLast = new RenderTexture(OutputPositions);
		Graphics.Blit(OutputPositions, OutputPositionsLast);

		var BoundsBox = GetComponent<BoxCollider>();
		if (BoundsBox != null)
		{
			var Min = BoundsBox.bounds.min;
			var Max = BoundsBox.bounds.max;
			Min = this.transform.TransformPoint(Min);
			Max = this.transform.TransformPoint(Max);
			AccumulatorMaterial.SetVector("WorldBoundsMin", Min);
			AccumulatorMaterial.SetVector("WorldBoundsMax", Max);
		}
	}

	void Update()
	{
		//BlitNextFrame();
	}

	public void OnFrame(PopCap.TFrameMeta ColourMeta, Texture ColourTexture, PopCap.TFrameMeta DepthMeta, Texture PositionTexture)
	{
		var RayMarchMaterial = AccumulatorMaterial;
		if (!this.isActiveAndEnabled)
			return;

		if (DepthMeta.Camera == null)
		{
			Debug.LogWarning("PointCloudRayMarch frame missing .Camera");
			return;
		}
		if (DepthMeta.Camera.Intrinsics == null)
		{
			Debug.LogWarning("PointCloudRayMarch frame missing .Camera.Intrinsics");
			return;
		}
		if (DepthMeta.Camera.LocalToWorld == null)
		{
			Debug.LogWarning("PointCloudRayMarch frame missing .Camera.LocalToWorld");
			return;
		}
		RayMarchMaterial.SetVector("CameraToLocalViewportMin", DepthMeta.Camera.GetCameraSpaceViewportMin());
		RayMarchMaterial.SetVector("CameraToLocalViewportMax", DepthMeta.Camera.GetCameraSpaceViewportMax());
		RayMarchMaterial.SetMatrix("CameraToLocalTransform", DepthMeta.Camera.GetCameraToLocal());
		RayMarchMaterial.SetMatrix("LocalToCameraTransform", DepthMeta.Camera.GetLocalToCamera());
		RayMarchMaterial.SetMatrix("WorldToLocalTransform", DepthMeta.Camera.GetWorldToLocal());

		bool WritePositions = true;

		if (WritePositions)
		{
			var PositionRT = PositionTexture as RenderTexture;
			RenderTexture.active = PositionRT;
			var Position2d = new Texture2D(PositionRT.width, PositionRT.height, TextureFormat.RGBAFloat, false);
			Position2d.ReadPixels(new Rect(0, 0, Position2d.width, Position2d.height), 0, 0);
			Position2d.Apply();
			var PositionPixels = Position2d.GetPixels();
			int INPUT_POSITION_COUNT = 90;
			var Positions4 = new Color[INPUT_POSITION_COUNT];

			int BlitCount = 1;
			for (int p=FirstPositionBlit;	p< PositionPixels.Length;	p+= INPUT_POSITION_COUNT, BlitCount++)
			{
				System.Array.Copy(PositionPixels, p, Positions4, 0, Positions4.Length);
				BlitNextFrame(Positions4);
				if (BlitCount >= MaxPositionBlits )
					break;
			}
			FirstPositionBlit += INPUT_POSITION_COUNT+ BlitCount;
			if (FirstPositionBlit > PositionPixels.Length)
				FirstPositionBlit = 0;
		}
		else
		{
			BlitNextFrame(null);
		}
	}

	void BlitNextFrame(Color[] InputPositions)
	{
		Graphics.Blit(OutputPositions, OutputPositionsLast);

		AccumulatorMaterial.SetTexture("PointCloudMapLastPositions", OutputPositionsLast);
		AccumulatorMaterial.SetFloat("BlitInitialise", 0.0f);

		if (InputPositions != null)
			AccumulatorMaterial.SetColorArray("InputPositions", InputPositions);

		Graphics.Blit(OutputPositionsLast, OutputPositions, AccumulatorMaterial);
	}

}
