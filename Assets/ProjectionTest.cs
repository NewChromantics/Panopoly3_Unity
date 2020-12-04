using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectionTest : MonoBehaviour
{
    public Camera VisualisationDepthCamera;
    public bool InvertZ = true;
    public bool InvertZAfter = true;
    public bool ApplyRotation = true;
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
        var LocalToWorld = Camera.GetLocalToWorld();

        //  https://github.com/sacchy/Unity-Arkit/blob/master/Assets/Plugins/iOS/UnityARKit/Utility/UnityARMatrixOps.cs
        VisualisationDepthCamera.transform.localPosition = Camera.GetPosition();
        VisualisationDepthCamera.transform.localRotation = Camera.GetRotation();
      


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
