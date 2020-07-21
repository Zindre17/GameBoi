using System;
using System.IO;
using System.Text;
using static WavSettings;

class Wav
{
    protected MemoryStream stream = new MemoryStream();

    private long streamPosition = 0;
    public Wav()
    {
        // Write header
        stream.Write(Encoding.ASCII.GetBytes(FILE_TYPE_ID));
        stream.Write(BitConverter.GetBytes(FILE_LENGTH));
        stream.Write(Encoding.ASCII.GetBytes(MEDIA_TYPE_ID));

        // write format
        stream.Write(Encoding.ASCII.GetBytes(FORMAT_ID));
        stream.Write(BitConverter.GetBytes(FORMAT_SIZE));
        stream.Write(BitConverter.GetBytes(FORMAT_TAG));
        stream.Write(BitConverter.GetBytes(CHANNELS));
        stream.Write(BitConverter.GetBytes(SAMPLE_RATE));
        stream.Write(BitConverter.GetBytes(AVG_BYTES_PER_SEC));
        stream.Write(BitConverter.GetBytes(BLOCK_ALIGN));
        stream.Write(BitConverter.GetBytes(BITS_PER_SAMPLE));

        //write data start
        stream.Write(Encoding.ASCII.GetBytes(DATA_ID));
        stream.Write(BitConverter.GetBytes(DATA_SIZE));

        stream.Position = streamPosition;
    }

    public MemoryStream Stream => stream;

    public void WriteNextSample(ushort channel1, ushort channel2, ushort channel3, ushort channel4)
    {
        streamPosition = stream.Position;
        stream.Position = stream.Length;
        stream.Write(BitConverter.GetBytes(channel1));
        stream.Write(BitConverter.GetBytes(channel2));
        stream.Write(BitConverter.GetBytes(channel3));
        stream.Write(BitConverter.GetBytes(channel4));
        stream.Position = streamPosition;
    }

    public void SetChannelSamples(short[] samples)
    {
        var startPos = stream.Position;
        stream.Position = DATA_SAMPLE_START_INDEX;
        int stepLength = (BITS_PER_SAMPLE / 8) * (CHANNELS);
        foreach (var sample in samples)
        {
            stream.Write(BitConverter.GetBytes(sample));
            stream.Position += stepLength;
        }
        stream.Position = startPos;
    }
}