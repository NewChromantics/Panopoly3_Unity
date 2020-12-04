using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectionTest : MonoBehaviour
{
    public Camera VisualisationDepthCamera;
    public bool ApplyTransform = true;
    PopCap.TCamera LastCamera;

    public void OnFrame(PopCap.TFrameMeta ColorMeta,Texture ColourText, PopCap.TFrameMeta DepthMeta,Texture DepthTexture)
    {
        if (DepthMeta.Camera != null)
        {
            LastCamera = DepthMeta.Camera;
            //UpdateCamera(DepthMeta.Camera);
        }
        else
        {
            Debug.Log("No camera in meta");
        }
    }

   

    void UpdateCamera(PopCap.TCamera Camera)
    {
        if (ApplyTransform)
        {
            var LocalToWorld = Camera.GetLocalToWorld();

            VisualisationDepthCamera.transform.localPosition = LocalToWorld.MultiplyPoint(Vector3.zero);
            VisualisationDepthCamera.transform.localRotation = LocalToWorld.rotation;
        }
    }


    void OnEnable()
	{
        //VisualisationDepthCamera = new Camera();
	}

	void Update()
    {
        if (LastCamera != null)
            UpdateCamera(LastCamera);
        
    }
}
