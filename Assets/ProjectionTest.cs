using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectionTest : MonoBehaviour
{
    public Camera VisualisationDepthCamera;
    public bool InvertZ = true;
    public bool InvertZAfter = true;
    public bool InvertLocalToWorld = false;
    public bool Transpose = false;
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

    public static Vector3 GetPosition(Matrix4x4 matrix)
    {
        // Convert from ARKit's right-handed coordinate
        // system to Unity's left-handed
        Vector3 position = matrix.GetColumn(3);
        position.z = -position.z;

        return position;
    }

    public static Quaternion GetRotation(Matrix4x4 matrix)
    {
        // Convert from ARKit's right-handed coordinate
        // system to Unity's left-handed
        Quaternion rotation = QuaternionFromMatrix(matrix);
        rotation.z = -rotation.z;
        rotation.w = -rotation.w;

        return rotation;
    }


    static Quaternion QuaternionFromMatrix(Matrix4x4 m)
    {
        // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
        Quaternion q = new Quaternion();
        q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
        q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
        q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
        q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
        q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
        q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
        q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));
        return q;
    }

    void UpdateCamera(PopCap.TCamera Camera)
    {
        var LocalToWorld = Camera.GetWorldToLocal();
        if (Transpose )
            LocalToWorld = LocalToWorld.transpose;  //  undo pop change
        if (InvertLocalToWorld)
            LocalToWorld = LocalToWorld.inverse;

        //  https://github.com/sacchy/Unity-Arkit/blob/master/Assets/Plugins/iOS/UnityARKit/Utility/UnityARMatrixOps.cs
        VisualisationDepthCamera.transform.localPosition = GetPosition(LocalToWorld);
        VisualisationDepthCamera.transform.localRotation = GetRotation(LocalToWorld);
        return;











        if (Transpose)
            LocalToWorld = LocalToWorld.transpose;

        var InvertZMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
        if (InvertZ)
        {
            if (InvertZAfter)
                LocalToWorld = LocalToWorld * InvertZMatrix;
            else
                LocalToWorld = InvertZMatrix * LocalToWorld;
        }

        if (InvertLocalToWorld)
            LocalToWorld = LocalToWorld.inverse;

        var Translation = LocalToWorld.MultiplyPoint(Vector3.zero); //(new Vector3(Row3.x, Row3.y, Row3.z)) / Row3.w;

        if (ApplyRotation)
        {
            /*
            if (!LocalToWorld.ValidTRS())
            {
                var Up = LocalToWorld.MultiplyPoint(Vector3.up);
                var Forward = LocalToWorld.MultiplyPoint(Vector3.forward);
                var Zero = LocalToWorld.MultiplyPoint(Vector3.zero);
                var Right = LocalToWorld.MultiplyPoint(Vector3.right);
                var NewTransform = Matrix4x4.LookAt(Zero, Forward, Up);
                VisualisationDepthCamera.transform.localRotation = NewTransform.rotation;
            }
            else*/
            {
                VisualisationDepthCamera.transform.localRotation = LocalToWorld.rotation;
            }
        }
        VisualisationDepthCamera.transform.localPosition = Translation;
      

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
