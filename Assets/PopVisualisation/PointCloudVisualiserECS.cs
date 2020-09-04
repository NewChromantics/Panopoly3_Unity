using System.Collections;
using System.Collections.Generic;
using PopCap;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

public class PointCloudVisualiserECS : MonoBehaviour
{
    [Range(0.05f, 1f)]
    [SerializeField]
    protected float m_Strength = 0.25f;
    
    [SerializeField] private Material depthMaterial;
    [SerializeField] private RenderTextureGPURequest renderTextureGPURequest;
    
    NativeArray<Vector3> m_Vertices;
    Vector3[] m_ModifiedVertices;

    CalculateJob m_CalculateJob;

    JobHandle m_JobHandle;

    MeshFilter m_MeshFilter;

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

    public PopCapFrameMeta LastMeta { get; set; }

    [BurstCompile]
    struct CalculateJob : IJobParallelFor
    {
        public NativeArray<Vector3> Vertices;
        public int Height;
        public int Width;
        public NativeArray<Color32> Colors;

        private float H, S, V;
        
        public void Execute(int i)
        {
            var vertex = Vertices[i];
            
            Color.RGBToHSV(Colors[i], out H, out S, out V);

            if (V > 0.8f)
            {
                // vertex.x = i * 0.001f;
                // vertex.y = i * 0.001f;
                vertex.z = H ;
            }
            else
            {
                // vertex.x = 0;
                // vertex.y = 0;
                vertex.z = -999f;
            }
            
            Vertices[i] = vertex;
        }
    }

    public void Update()
    {
        if(!_initMetaReceived) return;
        
        Graphics.Blit(null, _depthRenderTexture, depthMaterial);

        renderTextureGPURequest.RequestPixelData(_depthRenderTexture, RequestComplete);
        
        // RenderTexture.active = _depthRenderTexture;
        // _texture2D.ReadPixels(new Rect(0, 0, _width, _height), 0, 0, false);
        //
        // NativeArray<Color32> colors = _texture2D.GetRawTextureData<Color32>();
        //
        // RenderTexture.active = null;
        //
        // m_CalculateJob = new CalculateJob()
        // {
        //     Vertices = m_Vertices,
        //     Height = _height,
        //     Width = _width,
        //     Colors = colors
        // };
        //
        // m_JobHandle = m_CalculateJob.Schedule(m_Vertices.Length, 64);
    }

    private void RequestComplete(AsyncGPUReadbackRequest request)
    {
        NativeArray<Color32> colors = request.GetData<Color32>();
       
        RenderTexture.active = null;

        m_CalculateJob = new CalculateJob()
        {
            Vertices = m_Vertices,
            Height = _height,
            Width = _width,
            Colors = colors
        };

        m_JobHandle = m_CalculateJob.Schedule(m_Vertices.Length, 64);
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
        }
    }

    private IEnumerator Start()
    { 
        //Wait for first Frame
        while (LastMeta == null) { yield return null; }
        
        _height = LastMeta.Height;
        _width = LastMeta.Width;
        _points = _height * _width;
        
        InitializeMesh();

        _texture2D = new Texture2D(_width, _height);
        //debugMaterial.SetTexture(MainTex, _texture2D);
        
        _depthRenderTexture = new RenderTexture(_width, _height, 16, RenderTextureFormat.Default);
        RenderTexture.active = _depthRenderTexture;
        
        
        // this persistent memory setup assumes our vertex count will not expand
        m_Vertices = new NativeArray<Vector3>(_mesh.vertices, Allocator.Persistent);
        
        m_ModifiedVertices = new Vector3[m_Vertices.Length];

        _initMetaReceived = true;
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
                _verts[index].x = x * 0.001f;
                _verts[index].y = y * 0.001f;
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
        _mesh.MarkDynamic();
        gameObject.GetComponent<MeshFilter>().mesh = _mesh;
    }
    
    
 
    public void LateUpdate()
    {
        if(!_initMetaReceived) return;
        
        m_JobHandle.Complete();

        m_CalculateJob.Vertices.CopyTo(m_ModifiedVertices);

        _mesh.vertices = m_ModifiedVertices;
    }

    private void OnDestroy()
    {
        m_Vertices.Dispose();
    }
    
}
