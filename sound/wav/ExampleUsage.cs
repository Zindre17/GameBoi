using System.Collections.Generic;
using System.IO;
using System.Media;

class ExampleUse
{
    static SoundPlayer myAudio;
    private static byte[] myWaveData;

    // Sample rate (Or number of samples in one second)
    private const int SAMPLE_FREQUENCY = 44100;
    // 60 seconds or 1 minute of audio
    private const int AUDIO_LENGTH_IN_SECONDS = 1;

    public void Play()
    {
        List<byte> tempBytes = new List<byte>();

        WavHeader header = new WavHeader();
        WavFormatChunk format = new WavFormatChunk();
        WavDataChunk data = new WavDataChunk();

        // Create 1 second of tone at 697Hz
        SineGenerator leftData = new SineGenerator(697.0f,
           SAMPLE_FREQUENCY, AUDIO_LENGTH_IN_SECONDS);
        // Create 1 second of tone at 1209Hz
        SineGenerator rightData = new SineGenerator(1209.0f,
           SAMPLE_FREQUENCY, AUDIO_LENGTH_IN_SECONDS);

        data.AddSampleData(leftData.Data, rightData.Data);

        header.FileLength += format.Length() + data.Length();

        tempBytes.AddRange(header.GetBytes());
        tempBytes.AddRange(format.GetBytes());
        tempBytes.AddRange(data.GetBytes());

        myWaveData = tempBytes.ToArray();

        myAudio = new SoundPlayer(new MemoryStream(myWaveData));
        myAudio.PlayLooping();
    }
}