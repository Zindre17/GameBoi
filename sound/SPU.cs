using NAudio.Wave;
using static SoundRegisters;

public class SPU : Hardware
{
    private readonly NR50 nr50 = new NR50();
    private readonly NR51 nr51 = new NR51();
    private readonly NR52 nr52 = new NR52();

    private readonly Channel1 channel1;
    private readonly Channel2 channel2;
    private readonly Channel3 channel3;
    private readonly Channel4 channel4;

    private readonly WaveOut waveEmitter = new WaveOut();

    private readonly BufferedWaveProvider waveProvider;
    private readonly WaveFormat waveFormat;
    private static int samplesPerBatch;
    private static readonly int sampleBatchRate = 60;

    public SPU()
    {
        waveFormat = new WaveFormat();
        waveProvider = new BufferedWaveProvider(waveFormat)
        {
            BufferLength = waveFormat.BlockAlign * waveFormat.SampleRate,
            DiscardOnBufferOverflow = true
        };

        samplesPerBatch = waveProvider.BufferLength / (waveFormat.BlockAlign * sampleBatchRate);

        channel1 = new Channel1(nr52);
        channel2 = new Channel2(nr52);
        channel3 = new Channel3(nr52);
        channel4 = new Channel4(nr52);
    }

    public override void Connect(Bus bus)
    {
        this.bus = bus;

        bus.ReplaceMemory(NR50_address, nr50);
        bus.ReplaceMemory(NR51_address, nr51);
        bus.ReplaceMemory(NR52_address, nr52);

        channel1.Connect(bus);
        channel2.Connect(bus);
        channel3.Connect(bus);
        channel4.Connect(bus);

        waveEmitter.Init(waveProvider);
        waveEmitter.Play();
    }

    public void AddNextSamples()
    {
        var channel1Samples = channel1.GetNextSampleBatch(samplesPerBatch);
        var channel2Samples = channel2.GetNextSampleBatch(samplesPerBatch);
        var channel3Samples = channel3.GetNextSampleBatch(samplesPerBatch);
        var channel4Samples = channel4.GetNextSampleBatch(samplesPerBatch);

        var samples = new byte[samplesPerBatch * 4];

        var out1volume = nr50.GetVolumeScaler(true);
        var out2volume = nr50.GetVolumeScaler(false);

        int index = 0;
        for (int i = 0; i < samplesPerBatch; i++)
        {
            //channel1
            short c1Sample = 0;

            if (nr51.Is1Out1)
                c1Sample += (short)(channel1Samples[i] / 4);
            if (nr51.Is2Out1)
                c1Sample += (short)(channel2Samples[i] / 4);
            if (nr51.Is3Out1)
                c1Sample += (short)(channel3Samples[i] / 4);
            if (nr51.Is4Out1)
                c1Sample += (short)(channel4Samples[i] / 4);

            c1Sample = (short)(c1Sample * out1volume);

            samples[index++] = (byte)(c1Sample >> 8);
            samples[index++] = (byte)c1Sample;

            //channel2
            short c2Sample = 0;

            if (nr51.Is1Out2)
                c2Sample += (short)(channel1Samples[i] / 4);
            if (nr51.Is2Out2)
                c2Sample += (short)(channel2Samples[i] / 4);
            if (nr51.Is3Out2)
                c2Sample += (short)(channel3Samples[i] / 4);
            if (nr51.Is4Out2)
                c2Sample += (short)(channel4Samples[i] / 4);

            c2Sample = (short)(c2Sample * out2volume);

            samples[index++] = (byte)(c2Sample >> 8);
            samples[index++] = (byte)c2Sample;
        }

        waveProvider.AddSamples(samples, 0, samples.Length);
    }
}
