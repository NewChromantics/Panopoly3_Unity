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
		Graphics.Blit(Texture2D.blackTexture, OutputPositions);
		OutputPositionsLast = new RenderTexture(OutputPositions);
		Graphics.Blit(Texture2D.blackTexture, OutputPositionsLast);


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
		Graphics.Blit( OutputPositions, OutputPositionsLast);
		AccumulatorMaterial.SetTexture("PointCloudMapLastPositions", OutputPositionsLast);
		Graphics.Blit(OutputPositionsLast, OutputPositions, AccumulatorMaterial);
	}
}
