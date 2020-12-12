using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointCloudAccumulatorBlitter : MonoBehaviour
{
	public RenderTexture OutputPositions;
	RenderTexture OutputPositionsLast;
	public Material AccumulatorMaterial;

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

		BlitNextFrame();
	}

	void BlitNextFrame()
	{
		Graphics.Blit(OutputPositions, OutputPositionsLast);


		AccumulatorMaterial.SetTexture("PointCloudMapLastPositions", OutputPositionsLast);
		AccumulatorMaterial.SetFloat("WriteColourOutputInsteadOfPosition", 0.0f);
		AccumulatorMaterial.SetFloat("BlitInitialise", 0.0f);
		Graphics.Blit(OutputPositionsLast, OutputPositions, AccumulatorMaterial);
	}
}
