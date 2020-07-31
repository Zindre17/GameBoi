using static Frequencies;

abstract class SquareWaveChannel : SoundChannel, ISampleProvider
{
    protected Sweep sweep;
    protected WaveDuty waveDuty = new WaveDuty();
    protected Envelope envelope = new Envelope();
    protected FrequencyLow frequencyLow = new FrequencyLow();
    protected FrequencyHigh frequencyHigh = new FrequencyHigh();

    protected NR52 nr52;
    private byte channelBit;

    private SquareWaveProvider waveProvider = new SquareWaveProvider();

    public SquareWaveChannel(NR52 nr52, byte channelBit, bool hasSweep)
    {
        this.nr52 = nr52;
        this.channelBit = channelBit;

        if (hasSweep) sweep = new Sweep();

        lastFrequencyData = GetFrequencyData();
    }

    public short GetNextSample() => waveProvider.GetNextSample();

    public abstract override void Connect(Bus bus);

    private int elapsedSinceLastEnvelope = 0;
    private int elapsedSinceLastSweep = 0;

    private uint lastFrequencyData;
    private byte lastVolume;
    private byte lastDuty;

    private int elapsedDurationInCycles = 0;
    private int sweepSteps = 0;
    public override void Tick(Byte cycles)
    {
        if (frequencyHigh.IsInitial)
        {
            frequencyHigh.IsInitial = false;
            elapsedDurationInCycles = 0;
            sweepSteps = 0;
            double newDuration = frequencyHigh.HasDuration ? waveDuty.GetSoundLengthInMs() : 0;
            waveProvider.UpdateDuration(newDuration);
            waveProvider.Start();
        }

        if (IsEnvelopeActive())
        {
            elapsedSinceLastEnvelope += cycles;
            if (ShouldTriggerEnvelope())
            {
                envelope.InitialVolume += (envelope.IsIncrease ? 1 : -1);
                elapsedSinceLastEnvelope = 0;
            }
        }

        ushort frequencyData = GetFrequencyData();
        if (IsSweepActive())
        {
            elapsedSinceLastSweep += cycles;
            if (ShouldTriggerSweep())
            {
                frequencyData = sweep.GetFrequencyDataChange(frequencyData, ++sweepSteps);
                elapsedSinceLastSweep = 0;
            }
        }

        if (frequencyData != lastFrequencyData)
            waveProvider.UpdateFrequency(GetFrequency(frequencyData));

        if (waveDuty.Duty != lastDuty)
            waveProvider.UpdateDuty(waveDuty.GetDuty());

        if (envelope.InitialVolume != lastVolume)
            waveProvider.UpdateVolume(envelope.InitialVolume);

        lastFrequencyData = frequencyData;
        lastVolume = envelope.InitialVolume;
        lastDuty = waveDuty.Duty;
    }

    private bool IsEnvelopeActive()
    {
        if (envelope.LengthOfStep == 0) return false;
        if (envelope.InitialVolume == 0 && !envelope.IsIncrease) return false;
        return !(envelope.InitialVolume == 0x0F && envelope.IsIncrease);
    }

    private bool IsSweepActive()
    {
        if (sweep == null || sweep.SweepTime == 0) return false;
        return sweep.NrSweepShift != 0;
    }

    private bool ShouldTriggerEnvelope()
    {
        int triggerFrequency = 64 / envelope.LengthOfStep;
        int cyclesPerStep = (int)(cpuSpeed / triggerFrequency);

        return cyclesPerStep <= elapsedSinceLastEnvelope;
    }

    private bool ShouldTriggerSweep()
    {
        int triggerFrequency = 128 / sweep.SweepTime;
        int cyclesPerSweep = (int)(cpuSpeed / triggerFrequency);

        return cyclesPerSweep <= elapsedSinceLastSweep;
    }

    private ushort GetFrequencyData()
    {
        return (ushort)((frequencyHigh.HighBits << 8) | frequencyLow.LowBits);
    }

    private void SetFrequencyData(ushort newFrequencyData)
    {
        frequencyLow.Write((byte)newFrequencyData);
        frequencyHigh.HighBits = (newFrequencyData >> 8) & 7;
    }

    private ushort GetFrequencyData(uint frequency)
    {
        return (ushort)(0x800 - (0x20000 / frequency));
    }

    public uint GetFrequency() => GetFrequency(GetFrequencyData());

    private uint GetFrequency(ushort frequencyData)
    {
        return (uint)(0x20000 / (0x800 - frequencyData));
    }

}