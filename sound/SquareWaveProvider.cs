using static WavSettings;

class SquareWaveProvider
{
    private bool isStopped = true;

    private SquareWaveSettings settings = new SquareWaveSettings();

    public SquareWaveProvider() { }

    private class SquareWaveSettings
    {
        public int LowToHigh { get; private set; }
        public int DurationInSamples { get; private set; }
        public int SamplesPerPeriod { get; private set; }
        public Address Volume { get; private set; }
        public bool IsInitial { get; private set; }

        public SquareWaveSettings(uint frequency = 400, double duty = 0.5, ushort volume = 0, int durationInCycles = 0, bool isInitial = false)
        {
            DurationInSamples = durationInCycles;
            SamplesPerPeriod = System.Math.Max((int)(SAMPLE_RATE / frequency), 2);
            LowToHigh = (int)(SamplesPerPeriod * duty);
            Volume = volume;
            IsInitial = isInitial;

        }
    }

    public void UpdateSound(ulong atCpuCycle, uint frequency, double duty, Address volume, bool isInitial, int duration = 0)
    {
        settings = new SquareWaveSettings(
                frequency,
                duty,
                volume,
                isInitial ? duration : settings.DurationInSamples,
                isInitial
        );

        if (isInitial)
        {
            samplePoint = 0;
            samplesThisDuration = 0;
            Start();
        }
    }


    public void Start() => isStopped = false;
    public void Stop() => isStopped = true;

    private bool HasDuration => settings.DurationInSamples != 0;

    private ulong sampleNr = 0;
    private int samplePoint = 0;
    private long samplesThisDuration = 0;

    public short[] GetNextSampleBatch(int count)
    {
        short[] result = new short[count];

        if (HasDuration && samplesThisDuration >= settings.DurationInSamples)
            Stop();

        for (int i = 0; i < result.Length; i++)
            result[i] = GetNextSample();

        sampleNr += (uint)count;
        samplesThisDuration += count;

        return result;
    }
    private short GetNextSample()
    {
        short sample;
        if (isStopped || samplePoint == settings.LowToHigh || samplePoint == 0)
            sample = 0;
        else sample = (short)(settings.Volume * (samplePoint > settings.LowToHigh ? 1 : -1));

        samplePoint++;
        samplePoint %= settings.SamplesPerPeriod;

        return sample;
    }

}
