using NAudio.Wave;
using static SoundRegisters;
using static Frequencies;

class SPU : Hardware
{
    private NR52 nr52 = new NR52();

    private Channel1 channel1;
    private Channel2 channel2;
    private Channel4 channel4;

    private readonly WaveOut waveEmitter = new WaveOut();

    private BufferedWaveProvider waveProvider;
    private static int samplesPerBatch;

    public SPU()
    {
        var waveFormat = new WaveFormat();
        waveProvider = new BufferedWaveProvider(waveFormat);
        waveProvider.BufferLength = waveFormat.BlockAlign * waveFormat.SampleRate;
        waveProvider.DiscardOnBufferOverflow = true;

        samplesPerBatch = (int)(System.Math.Ceiling(waveProvider.BufferLength / 4000d));

        channel1 = new Channel1(nr52);
        channel2 = new Channel2(nr52);
        channel4 = new Channel4();
    }

    private static readonly ulong cyclesPerMs = (ulong)(System.Math.Ceiling(cpuSpeed / 1000d));
    private ulong lastClock;

    public override void Tick()
    {
        var clockNow = Cycles;
        var cycles = clockNow - lastClock;
        if (cycles >= cyclesPerMs)
        {
            AddNextSamples();
            lastClock = clockNow;
        }
    }

    public override void Connect(Bus bus)
    {
        this.bus = bus;

        bus.ReplaceMemory(NR52_address, nr52);

        channel1.Connect(bus);
        channel2.Connect(bus);

        channel1.Run();
        channel2.Run();
        channel4.Run();

        waveEmitter.Init(waveProvider);
        waveEmitter.Play();
    }

    public void AddNextSamples()
    {
        int bytesRead = 0;

        var channel1Samples = channel1.GetNextSampleBatch(samplesPerBatch);
        var channel2Samples = channel2.GetNextSampleBatch(samplesPerBatch);
        // var channel3Samples = channel3.GetNextSampleBatch(samplesPerBatch);
        var channel4Samples = channel4.GetNextSampleBatch(samplesPerBatch);
        var samples = new byte[samplesPerBatch * 4];

        for (int i = 0; i < channel2Samples.Length; i++)
        {
            short sample = 0;
            sample += (short)(channel1Samples[i] / 4);
            sample += (short)(channel2Samples[i] / 4);
            // sample += (short)(channel3Samples[i] / 4);
            sample += (short)(channel4Samples[i] / 4);

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
