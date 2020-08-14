using NAudio.Wave;
using static SoundRegisters;
using static Frequencies;
using System.Threading.Tasks;
using System.Threading;

class SPU : Hardware, IUpdateable
{
    private NR50 nr50 = new NR50();
    private NR51 nr51 = new NR51();
    private NR52 nr52 = new NR52();

    private Channel1 channel1;
    private Channel2 channel2;
    private Channel3 channel3;
    private Channel4 channel4;

    private readonly WaveOut waveEmitter = new WaveOut();

    private BufferedWaveProvider waveProvider;
    private readonly WaveFormat waveFormat;
    private static int samplesPerBatch;
    private static readonly double sampleBatchRate = 60d;

    public SPU()
    {
        waveFormat = new WaveFormat();
        waveProvider = new BufferedWaveProvider(waveFormat);
        waveProvider.BufferLength = waveFormat.BlockAlign * waveFormat.SampleRate;
        waveProvider.DiscardOnBufferOverflow = true;

        samplesPerBatch = (int)(System.Math.Ceiling(waveProvider.BufferLength / (waveFormat.BlockAlign * sampleBatchRate)));

        channel1 = new Channel1(nr52);
        channel2 = new Channel2(nr52);
        channel3 = new Channel3(nr52);
        channel4 = new Channel4(nr52);

    }

    private static readonly ulong cycelsBetweenBatches = (ulong)(System.Math.Ceiling(cpuSpeed / sampleBatchRate));
    private ulong elapsed;
    public void Update(byte cycles)
    {
        elapsed += cycles;

        if (elapsed < cycelsBetweenBatches) return;

        AddNextSamples(samplesPerBatch);

        if (waveEmitter.PlaybackState != PlaybackState.Playing)
            waveEmitter.Play();

        elapsed -= cycelsBetweenBatches;
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

    public void AddNextSamples(int sampleCount)
    {
        int index = 0;

        var channel1Samples = channel1.GetNextSampleBatch(sampleCount);
        var channel2Samples = channel2.GetNextSampleBatch(sampleCount);
        var channel3Samples = channel3.GetNextSampleBatch(sampleCount);
        var channel4Samples = channel4.GetNextSampleBatch(sampleCount);

        var samples = new byte[sampleCount * 4];

        var out1volume = nr50.GetVolumeScaler(true);
        var out2volume = nr50.GetVolumeScaler(false);

        for (int i = 0; i < sampleCount; i++)
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

            c2Sample = (short)(c2Sample * out1volume);

            samples[index++] = (byte)(c2Sample >> 8);
            samples[index++] = (byte)c2Sample;
        }

        waveProvider.AddSamples(samples, 0, samples.Length);
    }
}
