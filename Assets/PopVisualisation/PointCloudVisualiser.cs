using System;
using System.Collections;
using System.Collections.Generic;
using PopCap;
using UnityEngine;
using UnityEngine.Rendering;

public class PointCloudVisualiser : MonoBehaviour
{
    [SerializeField] private Material depthMaterial;
    [SerializeField] private Material debugMaterial;
    
    
    private RenderTexture _depthTexture2D;
    private int _width;
    private int _height;
    private int _points;

    private Mesh _mesh;
    private Vector3[] _verts;
    private Color32[] _colors;
    private int[] _indices;

    private Texture2D _texture2D;
    private RenderTexture _depthRenderTexture;
    
    private bool _initMetaReceived;
    private bool _updatePointCloud;
    
    private static readonly int DepthTex = Shader.PropertyToID("DepthTex");
    private static readonly int MainTex = Shader.PropertyToID("_MainTex");

    private PopCapFrameMeta LastMeta { get; set; }

    // Start is called before the first frame update

    private void Awake()
    {
       
    }

    IEnumerator Start()
    {
        //Wait for first Frame
        while (!_initMetaReceived) { yield return null; }
        
        InitializeMesh();
        
        Debug.Log("<color=green>" + _height + " : " + _width + "</color>");
        
        _texture2D = new Texture2D(_width, _height);
        //debugMaterial.SetTexture(MainTex, _texture2D);
        
        _depthRenderTexture = new RenderTexture(_width, _height, 16, RenderTextureFormat.Default);
        RenderTexture.active = _depthRenderTexture;

        _updatePointCloud = true;
    }

    private void InitializeMesh()
    {
        _mesh = new Mesh {indexFormat = IndexFormat.UInt32};

        _verts = new Vector3[_points];
        _colors = new Color32[_points];
        _indices = new int[_points];

        int index = 0;
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                _verts[index].x = x;
                _verts[index].y = y;
                _verts[index].z = 2;

                _colors[index].r = 0;
                _colors[index].g = 255;
                _colors[index].b = 255;
                _colors[index].a = 1;

                _indices[index] = index;
                index++;
            }
        }

        _mesh.vertices = _verts;
        _mesh.colors32 = _colors;
        _mesh.SetIndices(_indices, MeshTopology.Points, 0);
        gameObject.GetComponent<MeshFilter>().mesh = _mesh;
    }

    // Update is called once per frame

    void Update()
    {
        if (LastMeta == null) return;

        if (!_initMetaReceived)
        {
            _height = LastMeta.Height;
            _width = LastMeta.Width;
            _points = _height * _width;
            
            _initMetaReceived = true;
        }

        if (_updatePointCloud)
        {
 
        }
        
    }

    public void OnMeta(string MetaJson)
    {
        //	todo: store meta with frame counter
        //	this.LastMeta = Meta
        //	in decode;
        //	this.FrameMeta[FrameCounter] = LastMeta
        var NewMeta = JsonUtility.FromJson<PopCapFrameMeta>(MetaJson);

        if (NewMeta != null)
        {
            LastMeta = NewMeta;
            UpdatePointCloud();
        }
    }

    private void UpdatePointCloud()
    {
		if (!this.isActiveAndEnabled)
			return;
        Graphics.Blit(null, _depthRenderTexture, depthMaterial);
        RenderTexture.active = _depthRenderTexture;
        _texture2D.ReadPixels(new Rect(0, 0, _width, _height), 0, 0, false);
       _texture2D.Apply();
       
       Color32[] colors = _texture2D.GetPixels32();
       
       RenderTexture.active = null;
           
           int index = 0;
           for (int y = 0; y < _height; y++)
           {
               for (int x = 0; x < _width; x++)
               {
                   index = (y * _width) + x;
                   
                    float H, S, V;
                   
                    Color.RGBToHSV(colors[index], out H, out S, out V);
                   
                    if (V > 0.8f)
                    {
                        _verts[index].x = x * 0.001f;
                        _verts[index].y = y * 0.001f;
                        _verts[index].z = H ;
                    }
                    else
                    {
                        _verts[index].x = 0;
                        _verts[index].y = 0;
                        _verts[index].z = -999f;
                    }
                   
                    //index++;
               }
           }

           _mesh.vertices = _verts;
           _mesh.colors32 = colors;
           _mesh.RecalculateBounds();
    }
}
