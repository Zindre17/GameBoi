using System;
using static SoundRegisters;
using static WavSettings;

class Channel3 : SoundChannel
{
    private Register state = new MaskedRegister(0x7F);
    private Register soundLength = new Register();
    private Register outputLevel = new MaskedRegister(0x9F);
    private FrequencyLow frequencyLow = new FrequencyLow();
    private FrequencyHigh frequencyHigh = new FrequencyHigh();

    private WaveRam waveRam = new WaveRam();
    public Channel3(NR52 nr52) : base(nr52) { }
    public override void Connect(Bus bus)
    {
        this.bus = bus;

        bus.ReplaceMemory(NR30_address, state);
        bus.ReplaceMemory(NR31_address, soundLength);
        bus.ReplaceMemory(NR32_address, outputLevel);
        bus.ReplaceMemory(NR33_address, frequencyLow);
        bus.ReplaceMemory(NR34_address, frequencyHigh);

        bus.RouteMemory(WaveRam_address_start, waveRam, WaveRam_address_end);
    }

    private int currentFrequency = 1;
    private long currentLength;
    private long samplesThisLength;
    private bool isStopped = true;
    public void Update(byte cycles)
    {
        if (frequencyHigh.IsInitial)
        {
            nr52.TurnOn(2);
            frequencyHigh.IsInitial = false;
            if (frequencyHigh.HasDuration)
                currentLength = GetLengthInSamples();
            else
                currentLength = -1;
            samplesThisLength = 0;
            currentFrequency = GetFrequency();
            isStopped = false;
        }
        else
        {
            if (!GetState())
                isStopped = true;

            var newFrequency = GetFrequency();
            if (currentFrequency != newFrequency)
                currentFrequency = newFrequency;
        }
    }

    private bool GetState() => state.Read()[7];
    private long GetLengthInSamples()
    {
        var seconds = (0x100 - soundLength.Read()) * (1d / 0x100);
        return (long)(seconds * SAMPLE_RATE);
    }
    private int GetFrequency()
    {
        Byte low = frequencyLow.LowBits;
        Byte high = frequencyHigh.HighBits;
        Address fdata = (high << 8) | low;
        return 0x10000 / (0x800 - fdata);
    }

    private int GetVolume()
    {
        Byte volumeData = (outputLevel.Read() & 0x60) >> 5;
        if (volumeData == 0) return 0;
        if (volumeData == 1) return 64;
        if (volumeData == 2) return 32;
        return 16;
    }

    private int step = 0;
    private ulong sampleNr = 0;

    public short[] GetNextSampleBatch(int count)
    {
        Update(0);

        short[] samples = new short[count];

        if (!nr52.IsAllOn || isStopped)
            return samples;

        byte[] wavePattern = waveRam.GetSamples();

        double samplesPerStep = (SAMPLE_RATE / (double)currentFrequency) / wavePattern.Length;
        if (samplesPerStep == 0) samplesPerStep = 1;

        int volume = GetVolume();

        for (int i = 0; i < count; i++)
        {
            if (currentLength != -1 && i + samplesThisLength >= currentLength)
            {
                nr52.TurnOff(2);
                return samples;
            }
            else
            {
                step = (int)(sampleNr++ / samplesPerStep);
                step %= wavePattern.Length;
                var data = wavePattern[step];
                var normalized = (data / 7.5d) - 1;
                samples[i] = (short)(normalized * volume);
            }
        }
        sampleNr %= (ulong)(Math.Max(samplesPerStep * wavePattern.Length, 1));
        samplesThisLength += count;
        return samples;
    }
}