using System;
using System.Threading.Tasks;
using static Frequencies;

abstract class SquareWaveChannel : SoundChannel
{
    protected Sweep sweep;
    protected WaveDuty waveDuty = new WaveDuty();
    protected Envelope envelope = new Envelope();
    protected FrequencyLow frequencyLow = new FrequencyLow();
    protected FrequencyHigh frequencyHigh = new FrequencyHigh();

    protected byte channelBit;

    private SquareWaveProvider waveProvider = new SquareWaveProvider();

    public SquareWaveChannel(NR52 nr52, byte channelBit, bool hasSweep) : base(nr52)
    {
        this.channelBit = channelBit;

        if (hasSweep) sweep = new Sweep();

        waveProvider.OnDurationCompleted += () => nr52.TurnOff(channelBit);
    }

    private int samplesThisDuration = 0;
    public short[] GetNextSampleBatch(int count)
    {
        ushort frequencyData = GetFrequencyData();
        if (frequencyHigh.IsInitial)
        {
            frequencyHigh.IsInitial = false;
            nr52.TurnOn(channelBit);

            samplesThisDuration = 0;

            envelope.Initialize();

            int newDuration = frequencyHigh.HasDuration ? waveDuty.GetSoundLengthInSamples() : -1;

            waveProvider.UpdateSound(
                GetFrequency(frequencyData),
                waveDuty.GetDuty(),
                true,
                newDuration
            );
        }
        else
        {
            if (sweep != null)
                frequencyData = sweep.GetFrequencyAfterSweep(frequencyData, samplesThisDuration);

            waveProvider.UpdateSound(
                GetFrequency(frequencyData),
                waveDuty.GetDuty(),
                false
            );
        }

        short[] samples = new short[count];
        if (nr52.IsAllOn)
        {
            for (int i = 0; i < count; i++)
            {
                samples[i] = (short)(waveProvider.GetSample(samplesThisDuration) * envelope.GetVolume(samplesThisDuration));
                samplesThisDuration++;
            }
        }

        return samples;
    }

    public abstract override void Connect(Bus bus);


    private ushort GetFrequencyData()
    {
        return (ushort)((frequencyHigh.HighBits << 8) | frequencyLow.LowBits);
    }

    private ushort GetFrequencyData(uint frequency)
    {
        return (ushort)(0x800 - (0x20000 / frequency));
    }

    private uint GetFrequency(ushort frequencyData)
    {
        return (uint)(0x20000 / (0x800 - frequencyData));
    }

}