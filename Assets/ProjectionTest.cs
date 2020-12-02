using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectionTest : MonoBehaviour
{
    public Camera VisualisationDepthCamera;
    public bool TransposeLocalToWorld = false;
    public bool ApplyRotation = true;

    public void OnFrame(PopCap.TFrameMeta ColorMeta,Texture ColourText, PopCap.TFrameMeta DepthMeta,Texture DepthTexture)
    {
        if (DepthMeta.Camera != null)
        {
            UpdateCamera(DepthMeta.Camera);
        }
        else
        {
            Debug.Log("No camera in meta");
        }
    }

    void UpdateCamera(PopCap.TCamera Camera)
    {
        if (Camera.LocalToWorld.Length != 16)
        {
            Debug.LogError("Camera meta LocalToWorld length is " + Camera.LocalToWorld.Length);
            return;
        }
        var Row0 = new Vector4(Camera.LocalToWorld[0], Camera.LocalToWorld[1], Camera.LocalToWorld[2], Camera.LocalToWorld[3]);
        var Row1 = new Vector4(Camera.LocalToWorld[4], Camera.LocalToWorld[5], Camera.LocalToWorld[6], Camera.LocalToWorld[7]);
        var Row2 = new Vector4(Camera.LocalToWorld[8], Camera.LocalToWorld[9], Camera.LocalToWorld[10], Camera.LocalToWorld[11]);
        var Row3 = new Vector4(Camera.LocalToWorld[12], Camera.LocalToWorld[13], Camera.LocalToWorld[14], Camera.LocalToWorld[15]);
        var LocalToWorld = new Matrix4x4(Row0,Row1,Row2,Row3);
        if (TransposeLocalToWorld)
            LocalToWorld = LocalToWorld.transpose;

        
        var Translation = LocalToWorld.MultiplyPoint(Vector3.zero); //(new Vector3(Row3.x, Row3.y, Row3.z)) / Row3.w;

        if (ApplyRotation )
            VisualisationDepthCamera.transform.localRotation = LocalToWorld.rotation;

        VisualisationDepthCamera.transform.localPosition = Translation;

    }


    void OnEnable()
	{
        //VisualisationDepthCamera = new Camera();
	}

	void Update()
    {
        
    }
}
