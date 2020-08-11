using NAudio.Wave;
using static SoundRegisters;
using static Frequencies;
using System.Threading.Tasks;
using System.Threading;

class SPU : Hardware, IUpdateable
{
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
        elapsed -= cycelsBetweenBatches;
    }

    public override void Connect(Bus bus)
    {
        this.bus = bus;

        bus.ReplaceMemory(NR52_address, nr52);

        channel1.Connect(bus);
        channel2.Connect(bus);
        channel3.Connect(bus);

        waveEmitter.Init(waveProvider);
        waveEmitter.Play();
    }

    public void AddNextSamples(int sampleCount)
    {
        int bytesRead = 0;

        var channel1Samples = channel1.GetNextSampleBatch(sampleCount);
        var channel2Samples = channel2.GetNextSampleBatch(sampleCount);
        var channel3Samples = channel3.GetNextSampleBatch(sampleCount);
        // var channel4Samples = channel4.GetNextSampleBatch(sampleCount);

        var samples = new byte[sampleCount * 4];

        for (int i = 0; i < sampleCount; i++)
        {
            short sample = 0;
            // sample += (short)(channel1Samples[i] / 4);
            // sample += (short)(channel2Samples[i] / 4);
            sample += (short)(channel3Samples[i] / 4);
            // sample += (short)(channel4Samples[i] / 4);

            byte high = (byte)(sample >> 8);
            byte low = (byte)sample;

            //channel1
            samples[bytesRead++] = high;
            samples[bytesRead++] = low;
            //channel2
            samples[bytesRead++] = high;
            samples[bytesRead++] = low;
        }

        waveProvider.AddSamples(samples, 0, samples.Length);
    }
}
