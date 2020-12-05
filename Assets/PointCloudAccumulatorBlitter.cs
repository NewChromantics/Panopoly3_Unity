using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PointCloudAccumulatorBlitter : MonoBehaviour
{
	public RenderTexture OutputPositions;
	public RenderTexture OutputColours;
	RenderTexture OutputPositionsLast;
	RenderTexture OutputColoursLast;
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
		Graphics.Blit(Texture2D.blackTexture, OutputPositions);
		OutputPositionsLast = new RenderTexture(OutputPositions);
		Graphics.Blit(Texture2D.blackTexture, OutputPositionsLast);

		Graphics.Blit(Texture2D.blackTexture, OutputColours);
		OutputColoursLast = new RenderTexture(OutputColours);
		Graphics.Blit(Texture2D.blackTexture, OutputColoursLast);


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
		Graphics.Blit(OutputPositions, OutputPositionsLast);
		Graphics.Blit(OutputColours, OutputColoursLast);

		//	gr: instead of MRT, do two passes. One updates colours and emulates new vs old change
		//		2nd does the same but then writes positions
		AccumulatorMaterial.SetTexture("PointCloudMapLastPositions", OutputPositionsLast);
		AccumulatorMaterial.SetTexture("PointCloudMapLastColours", OutputColoursLast);
		AccumulatorMaterial.SetFloat("WriteColourOutputInsteadOfPosition", 1.0f);
		Graphics.Blit(null, OutputColours, AccumulatorMaterial);

		AccumulatorMaterial.SetTexture("PointCloudMapLastPositions", OutputPositionsLast);
		AccumulatorMaterial.SetTexture("PointCloudMapLastColours", OutputColoursLast);
		AccumulatorMaterial.SetFloat("WriteColourOutputInsteadOfPosition", 0.0f);
		Graphics.Blit(null, OutputPositions, AccumulatorMaterial);
	}
}
