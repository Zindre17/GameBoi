using System;
using System.Collections.Generic;
using System.Text;

class WavFormatChunk
{
    private ushort _bitsPerSample;
    private ushort _channels;
    private uint _frequency;
    private const string CHUNK_ID = "fmt ";

    public string ChunkId { get; private set; }
    public uint ChunkSize { get; private set; }
    public ushort FormatTag { get; private set; }

    public ushort Channels
    {
        get { return _channels; }
        set { _channels = value; RecalcBlockSizes(); }
    }

    public uint Frequency
    {
        get { return _frequency; }
        set { _frequency = value; RecalcBlockSizes(); }
    }

    public uint AverageBytesPerSec { get; private set; }
    public ushort BlockAlign { get; private set; }

    public ushort BitsPerSample
    {
        get { return _bitsPerSample; }
        set { _bitsPerSample = value; RecalcBlockSizes(); }
    }

    public WavFormatChunk()
    {
        ChunkId = CHUNK_ID;
        ChunkSize = 16;
        FormatTag = 1;       // MS PCM (Uncompressed wave file)
        Channels = 2;        // Default to stereo
        Frequency = 44100;   // Default to 44100hz
        BitsPerSample = 16;  // Default to 16bits
        RecalcBlockSizes();
    }

    private void RecalcBlockSizes()
    {
        BlockAlign = (ushort)(_channels * (_bitsPerSample / 8));
        AverageBytesPerSec = _frequency * BlockAlign;
    }

    public byte[] GetBytes()
    {
        List<byte> chunkBytes = new List<byte>();

        chunkBytes.AddRange(Encoding.ASCII.GetBytes(ChunkId));
        chunkBytes.AddRange(BitConverter.GetBytes(ChunkSize));
        chunkBytes.AddRange(BitConverter.GetBytes(FormatTag));
        chunkBytes.AddRange(BitConverter.GetBytes(Channels));
        chunkBytes.AddRange(BitConverter.GetBytes(Frequency));
        chunkBytes.AddRange(BitConverter.GetBytes(AverageBytesPerSec));
        chunkBytes.AddRange(BitConverter.GetBytes(BlockAlign));
        chunkBytes.AddRange(BitConverter.GetBytes(BitsPerSample));

        return chunkBytes.ToArray();
    }

    public uint Length()
    {
        return (uint)GetBytes().Length;
    }

}