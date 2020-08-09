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

    Task updater = null;
    public short[] GetNextSampleBatch(int count)
    {
        if (updater == null || updater.Status == TaskStatus.RanToCompletion)
        {
            updater = new Task(() => Update(0));
            updater.Start();
        }
        return waveProvider.GetNextSampleBatch(count);
    }

    public abstract override void Connect(Bus bus);

    private uint lastFrequencyData;
    private byte lastVolume;
    private byte lastDuty;

    private ulong lastClock;
    private ulong lastInitial;

    public void Update(byte _)
    {
        ulong newClock = Cycles;

        ushort frequencyData = GetFrequencyData();

        if (frequencyHigh.IsInitial)
        {
            frequencyHigh.IsInitial = false;
            lastInitial = newClock;
            int newDuration = frequencyHigh.HasDuration ? waveDuty.GetSoundLengthInSamples() : 0;
            waveProvider.UpdateSound(
                GetFrequency(frequencyData),
                waveDuty.GetDuty(),
                envelope.GetVolume(),
                true,
                newDuration
            );

            lastVolume = envelope.InitialVolume;
        }
        else
        {
            Byte volume = envelope.InitialVolume;
            Byte stepLengh = envelope.LengthOfStep;
            bool isIncrease = envelope.IsIncrease;

            if (stepLengh != 0)
            {
                double triggerFrequency = 64d / stepLengh;
                int cyclesPerStep = (int)(cpuSpeed / triggerFrequency);

                long duration = (long)(newClock - lastInitial);
                int steps = (int)(duration / cyclesPerStep);
                if (steps > 0)
                    if (isIncrease)
                    {
                        volume = Math.Min(Envelope.MaxVolume, volume + steps);
                    }
                    else
                    {
                        volume = Math.Max(0, volume - steps);
                    }
            }

            if (sweep != null)
            {

                var sweepShifts = sweep.NrSweepShift;
                var sweepTime = sweep.SweepTime;
                var isSubtraction = sweep.IsSubtraction;
                if (sweepTime != 0 && sweepShifts != 0)
                {
                    int triggerFrequency = 128 / sweepTime;
                    int cyclesPerSweep = (int)(cpuSpeed / triggerFrequency);

                    long duration = (long)(newClock - lastInitial);
                    int steps = (int)(duration / cyclesPerSweep);

                    if (steps > 0)
                        frequencyData = sweep.GetFrequencyDataChange(frequencyData, steps, sweepShifts, isSubtraction);
                }
            }

            if (frequencyData != lastFrequencyData || waveDuty.Duty != lastDuty || envelope.InitialVolume != lastVolume)
                waveProvider.UpdateSound(
                    GetFrequency(frequencyData),
                    waveDuty.GetDuty(),
                    envelope.GetVolume(volume),
                    false
                );

            lastVolume = volume;
        }

        lastFrequencyData = frequencyData;
        lastDuty = waveDuty.Duty;
    }

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