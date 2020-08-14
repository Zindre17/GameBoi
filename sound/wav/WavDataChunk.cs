using System;
using System.Collections.Generic;
using System.Text;

public class WavDataChunk
{
    private const string CHUNK_ID = "data";

    public string ChunkId { get; private set; }
    public uint ChunkSize { get; set; }
    public short[] WaveData { get; private set; }

    public WavDataChunk()
    {
        ChunkId = CHUNK_ID;
        ChunkSize = 0;  // Until we add some data
    }

    public uint Length()
    {
        return (uint)GetBytes().Length;
    }

    public byte[] GetBytes()
    {
        List<byte> chunkBytes = new List<byte>();

        chunkBytes.AddRange(Encoding.ASCII.GetBytes(ChunkId));
        chunkBytes.AddRange(BitConverter.GetBytes(ChunkSize));
        byte[] bufferBytes = new byte[WaveData.Length * 2];
        Buffer.BlockCopy(WaveData, 0, bufferBytes, 0,
           bufferBytes.Length);
        chunkBytes.AddRange(bufferBytes);

        return chunkBytes.ToArray();
    }

    public void AddSampleData(short[] leftBuffer,
       short[] rightBuffer)
    {
        WaveData = new short[leftBuffer.Length +
           rightBuffer.Length];
        int bufferOffset = 0;
        for (int index = 0; index < WaveData.Length; index += 2)
        {
            WaveData[index] = leftBuffer[bufferOffset];
            WaveData[index + 1] = rightBuffer[bufferOffset];
            bufferOffset++;
        }
        ChunkSize = (uint)WaveData.Length * 2;
    }

}