using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class PointCloudAccumulatorBruteForce : MonoBehaviour
{
    Texture2D LastPositionTexture;
    Texture2D AccumulatedPositionsBuffer;
    public RenderTexture AccumulatedPositions;  //  output
    public BoxCollider WorldBoundsBox;

    System.Threading.Thread UpdateAccumulationThread;
    ProcessData? ThreadPostProcessData = null;      //  if not null we need to finish this data

    struct ProcessData
    {
        public int BlockWidth;
        public int BlockHeight;
        public int BlockDepth;
        public Vector3 WorldMin;
        public Vector3 WorldMax;
        public Color[] FramePositions;
        public Color[] BufferPositions;
        public int BufferWidth;
        public int BufferHeight;
    };

    class BlockMapMeta_t
    {
        float BlockSize = 1.0f;      //  NxNxN world units
        int PointsPerBlock = 100;  //  x4 for vector4, use last value for counter
        public readonly Vector3 WorldMin = new Vector3(-1, -1, 0);
        public readonly Vector3 WorldMax = new Vector3(1, 1, 2);
        public Color[] BlockDatas;

        //  caches
        public readonly int BlockW;
        public readonly int BlockH;
        public readonly int BlockD;

        public readonly int BlockTotalDataDim; //   NxN for texture size includes padding so we have a square texture  
        public int BlockTotalDataSize { get { return BlockTotalDataDim * BlockTotalDataDim; } }

        public readonly Dictionary<int, int> DirtyBlockIndexes = new Dictionary<int, int>();

        public BlockMapMeta_t()
        {
            BlockW = Mathf.CeilToInt((WorldMax.x - WorldMin.x) / BlockSize);
            BlockH = Mathf.CeilToInt((WorldMax.y - WorldMin.y) / BlockSize);
            BlockD = Mathf.CeilToInt((WorldMax.z - WorldMin.z) / BlockSize);

            var TotalSize = BlockW * BlockH * BlockD * PointsPerBlock;
            var Width = Mathf.CeilToInt(Mathf.Sqrt(TotalSize));
            Width = Mathf.NextPowerOfTwo(Width);
            BlockTotalDataDim = Width;

            //BlockDatas = new Color[BlockW * BlockH * BlockD];
            BlockDatas = new Color[BlockTotalDataSize];
        }

        public int GetBlockIndex(int BlockX, int BlockY, int BlockZ)
        {
            int Blocki = 0;
            Blocki += BlockX * 1;
            Blocki += BlockY * (BlockW);
            Blocki += BlockZ * (BlockW * BlockH);

            return Blocki;
        }

        public void AddPosition(int BlockX, int BlockY, int BlockZ, Color Position)
        {
            if (BlockX < 0 || BlockX >= BlockW || BlockY < 0 || BlockY >= BlockH || BlockZ < 0 || BlockZ >= BlockD)
                return;
            var BlockIndex = GetBlockIndex(BlockX, BlockY, BlockZ);
            var BlockDataIndex = BlockIndex * PointsPerBlock;
            var BlockCount4 = BlockDatas[BlockDataIndex + PointsPerBlock - 1];
            var BlockCount = (int)(BlockCount4.r);
            var WriteIndex = Mathf.Min(BlockCount, PointsPerBlock - 1);
            BlockDatas[BlockDataIndex + WriteIndex] = Position;

            //if (!DirtyBlockIndexes.ContainsKey(BlockIndex))
            //   DirtyBlockIndexes.Add(BlockIndex, 0);

            //  update counter
            BlockCount++;
            BlockCount4.r = BlockCount;
            BlockDatas[BlockDataIndex + PointsPerBlock - 1] = BlockCount4;
        }
    }
    BlockMapMeta_t BlockMapMeta = new BlockMapMeta_t();


    static float Range(float Min, float Max, float Value)
    {
        return (Value - Min) / (Max - Min);
    }

    void WritePosition(Color Position)
    {
        var Valid = Position.a > 0.5f;
        var xf = Range(BlockMapMeta.WorldMin.x, BlockMapMeta.WorldMax.x, Position.r);
        var yf = Range(BlockMapMeta.WorldMin.y, BlockMapMeta.WorldMax.y, Position.g);
        var zf = Range(BlockMapMeta.WorldMin.z, BlockMapMeta.WorldMax.z, Position.b);
        var bx = Mathf.FloorToInt(BlockMapMeta.BlockW * xf);
        var by = Mathf.FloorToInt(BlockMapMeta.BlockH * yf);
        var bz = Mathf.FloorToInt(BlockMapMeta.BlockD * zf);
        BlockMapMeta.AddPosition(bx, by, bz, Position);
    }

    ProcessData PreProcess()
    {
        Debug.Log("PreProcess");
        var Data = new ProcessData();
        if (AccumulatedPositionsBuffer == null)
        {
            AccumulatedPositionsBuffer = new Texture2D(AccumulatedPositions.width, AccumulatedPositions.height, TextureFormat.RGBAFloat, false);
        }
        Data.BufferPositions = AccumulatedPositionsBuffer.GetPixels();

        var Min = WorldBoundsBox.bounds.min;
        var Max = WorldBoundsBox.bounds.max;
        Data.WorldMin = WorldBoundsBox.transform.TransformPoint(Min);
        Data.WorldMax = WorldBoundsBox.transform.TransformPoint(Max);

        Data.BlockWidth = AccumulatedPositionsBuffer.width;
        Data.BufferWidth = AccumulatedPositionsBuffer.width;
        Data.BufferHeight = AccumulatedPositionsBuffer.height;
        Data.BlockDepth = Mathf.FloorToInt(Mathf.Sqrt(Data.BufferHeight));
        Data.BlockHeight = (int)(Data.BufferHeight / Data.BlockDepth);

        Data.FramePositions = LastPositionTexture.GetPixels();

        return Data;
     }

    void PostProcess(ProcessData Data)
	{
        Debug.Log("Post process");
        AccumulatedPositionsBuffer.SetPixels(Data.BufferPositions);
        AccumulatedPositionsBuffer.Apply();

        Graphics.Blit(AccumulatedPositionsBuffer, AccumulatedPositions);
    }

    static void Process(ProcessData Data)
    {
        Debug.Log("Running Process");
        

       
        System.Func<int,int,Vector3> PointCloudMapUvToXyz = (int px,int py) =>
		{
            /* shader
                int x = uv.x * BLOCKWIDTH;
                int Row = uv.y * BLOCKHEIGHT * BLOCKDEPTH;
                int y = Row % BLOCKHEIGHT;
                int z = Row / BLOCKHEIGHT;
                 return int3(x, y, z);
            */
            float uv_y = py / (float)Data.BufferHeight;
            int Row = (int)(uv_y * Data.BlockHeight * Data.BlockDepth);
            int x = px;
            int y = Row % Data.BlockHeight;
            int z = Row / Data.BlockHeight;

            //  convert to world
            float Worldu = x / (float)(Data.BlockWidth - 1);
            float Worldv = y / (float)(Data.BlockHeight - 1);
            float Worldw = z / (float)(Data.BlockDepth - 1);
            var Worldx = Mathf.Lerp(Data.WorldMin.x, Data.WorldMax.x, Worldu);
            var Worldy = Mathf.Lerp(Data.WorldMin.y, Data.WorldMax.y, Worldv);
            var Worldz = Mathf.Lerp(Data.WorldMin.z, Data.WorldMax.z, Worldw);
            return new Vector3(Worldx, Worldy, Worldz);
        };

        System.Func<Vector3,float> GetNearestDistance = (Vector3 Position)=>
		{
            var TestPosition = new Vector3();
            var BestDistance = 999.0f;
            for ( int i=0;  i<Data.FramePositions.Length; i++ )
			{
                var lpxyz = Data.FramePositions[i];
                TestPosition.x = lpxyz.r;
                TestPosition.y = lpxyz.g;
                TestPosition.z = lpxyz.b;
                var Distance = Vector3.Distance(Position, TestPosition);
                BestDistance = Mathf.Min(BestDistance, Distance);
            }
            return BestDistance;
        };

        //  update each map pixel with a new distance
        for (var mpy = 0; mpy < Data.BufferHeight; mpy++)
        {
            var OutputPixel = new Color();
            Debug.Log("Accumulation Row " + mpy);
            for (var mpx = 0; mpx < Data.BufferWidth; mpx++)
            {
                var Mapxyz = PointCloudMapUvToXyz(mpx, mpy);
                var Distance = GetNearestDistance(Mapxyz);
                OutputPixel.r = Distance;
                OutputPixel.g = Distance;
                OutputPixel.b = Distance;
                OutputPixel.a = Distance;
                int i = (mpy * Data.BufferWidth) +mpx;
                Data.BufferPositions[i] = OutputPixel;
            }
        }
        
    }


    public void OnPositionTexture(RenderTexture PositionTexture)
	{
        if (!this.isActiveAndEnabled)
            return;

        //  busy processing
        if (UpdateAccumulationThread != null)
            return;

        if (LastPositionTexture == null)
        {
            LastPositionTexture = new Texture2D(PositionTexture.width, PositionTexture.height, TextureFormat.RGBAFloat, false);
        }

        RenderTexture.active = PositionTexture;
        LastPositionTexture.ReadPixels(new Rect(0, 0, LastPositionTexture.width, LastPositionTexture.height), 0, 0);
        LastPositionTexture.Apply();
    }

    public void OnFrame(PopCap.TFrameMeta ColorMeta, Texture ColourText, PopCap.TFrameMeta DepthMeta, Texture DepthTexture)
    {
        if (!this.isActiveAndEnabled)
			return;
    }

    void StartAccumulationThread()
    {
        Debug.Log("StartAccumulationThread");
        var ProcessData = PreProcess();
        System.Action Lambda = () =>
        {
            Debug.Log("Thread Started");
            Process(ProcessData);
            
            UpdateAccumulationThread = null;
            LastPositionTexture = null;
            ThreadPostProcessData = ProcessData;
            Debug.Log("Thread finished");
        };
        UpdateAccumulationThread = new System.Threading.Thread(new System.Threading.ThreadStart(Lambda));
        UpdateAccumulationThread.Start();
    }


    void Update()
	{
        //  post process data
        if (ThreadPostProcessData.HasValue )
		{
            PostProcess(ThreadPostProcessData.Value);
            ThreadPostProcessData = null;
        }
            //  processing 
        if (UpdateAccumulationThread != null)
            return;

        if (LastPositionTexture == null)
            return;

        //  start new thread as we have new work to do
        StartAccumulationThread();
	}
}
