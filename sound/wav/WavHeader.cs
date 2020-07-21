using System;
using System.Collections.Generic;
using System.Text;

class WavHeader
{
    private const string FILE_TYPE_ID = "RIFF";
    private const string MEDIA_TYPE_ID = "WAVE";

    public string FileTypeId { get; private set; }
    public uint FileLength { get; set; }
    public string MediaTypeId { get; private set; }

    public WavHeader()
    {
        FileTypeId = FILE_TYPE_ID;
        MediaTypeId = MEDIA_TYPE_ID;
        // Minimum size is always 4 bytes
        FileLength = 4;
    }

    public byte[] GetBytes()
    {
        List<byte> chunkData = new List<byte>();
        chunkData.AddRange(Encoding.ASCII.GetBytes(FileTypeId));
        chunkData.AddRange(BitConverter.GetBytes(FileLength));
        chunkData.AddRange(Encoding.ASCII.GetBytes(MediaTypeId));
        return chunkData.ToArray();
    }

}