using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class CloudAccumulator : MonoBehaviour
{
    Texture2D LastPositionTexture;
    Color[] LastPositions;
    public Texture2D AccumulatedPositionsBuffer;
    public RenderTexture AccumulatedPositions;  //  output

    class BlockMapMeta_t
    {
        float BlockSize = 0.01f;      //  NxNxN world units
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
            Blocki += BlockZ * (BlockW* BlockH);

            return Blocki;
        }

        public void AddPosition(int BlockX, int BlockY, int BlockZ,Color Position)
		{
            if (BlockX < 0 || BlockX >= BlockW || BlockY < 0 || BlockY >= BlockH || BlockZ < 0 || BlockZ >= BlockD)
                return;
            var BlockIndex = GetBlockIndex(BlockX, BlockY, BlockZ);
            var BlockDataIndex = BlockIndex * PointsPerBlock;
            var BlockCount4 = BlockDatas[BlockDataIndex + PointsPerBlock - 1];
            var BlockCount = (int)(BlockCount4.r);
            var WriteIndex = Mathf.Min(BlockCount, PointsPerBlock - 1);
            BlockDatas[BlockDataIndex+WriteIndex] = Position;

            //if (!DirtyBlockIndexes.ContainsKey(BlockIndex))
             //   DirtyBlockIndexes.Add(BlockIndex, 0);

            //  update counter
            BlockCount++;
            BlockCount4.r = BlockCount;
            BlockDatas[BlockDataIndex + PointsPerBlock - 1] = BlockCount4;
        }
    }
    BlockMapMeta_t BlockMapMeta = new BlockMapMeta_t();


    static float Range(float Min,float Max,float Value)
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

    void UpdateAcculumation()
    {
        //  todo a gpu version which calcs blocks that can be touched, blit to each one and read values out of position texture in camera space
        for ( int pi=0; pi<LastPositions.Length;    pi++ )
		{
            var PositionColour = LastPositions[pi];
            WritePosition(PositionColour);
        }

        if(false)
        {
            var Keys = BlockMapMeta.DirtyBlockIndexes.Keys.ToList();
            var KeyNames = "";
            foreach (var Key in Keys)
                KeyNames += Key + " ";
            Debug.Log("Changed block indexes;" + KeyNames);
        }

        //  update texture
        if (AccumulatedPositionsBuffer == null )
		{
            AccumulatedPositionsBuffer = new Texture2D(BlockMapMeta.BlockTotalDataDim, BlockMapMeta.BlockTotalDataDim, TextureFormat.RGBAFloat, false);
        }
        AccumulatedPositionsBuffer.SetPixels(BlockMapMeta.BlockDatas);
        AccumulatedPositionsBuffer.Apply();
        if (AccumulatedPositions != null)
        {
            if (AccumulatedPositions.width != AccumulatedPositionsBuffer.width || AccumulatedPositions.height != AccumulatedPositionsBuffer.height)
                Debug.LogError("Need render texture to be " + AccumulatedPositionsBuffer.width + "x" + AccumulatedPositionsBuffer.height);
            Graphics.Blit(AccumulatedPositionsBuffer, AccumulatedPositions);
            
        }
    }


    public void OnPositionTexture(RenderTexture PositionTexture)
	{
        if (LastPositionTexture == null)
        {
            LastPositionTexture = new Texture2D(PositionTexture.width, PositionTexture.height, TextureFormat.RGBAFloat, false);
        }

        RenderTexture.active = PositionTexture;
        LastPositionTexture.ReadPixels(new Rect(0, 0, LastPositionTexture.width, LastPositionTexture.height), 0, 0);
        LastPositionTexture.Apply();
        LastPositions = LastPositionTexture.GetPixels();
    }

    public void OnFrame(PopCap.TFrameMeta ColorMeta, Texture ColourText, PopCap.TFrameMeta DepthMeta, Texture DepthTexture)
    {
    }

	void Update()
	{
        if (LastPositionTexture != null)
            UpdateAcculumation();
	}
}
