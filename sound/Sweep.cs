using static WavSettings;

class Sweep : MaskedRegister
{
    public Sweep() : base(0x80) { }

    private Byte SweepTime => (data & 0x70) >> 4;
    private bool IsSubtraction => data[3];
    private Byte NrSweepShift => data & 7;

    public delegate void OnSweepOverflow();
    public OnSweepOverflow OverflowListeners;

    public Address GetFrequencyAfterSweep(Address frequencyData, int sampleThisDuration)
    {
        Address result = frequencyData;
        if (IsActive)
        {
            var freq = 128 / SweepTime;
            var samplesPerStep = SAMPLE_RATE / freq;
            int steps = (int)(sampleThisDuration / samplesPerStep);

            var alteration = (1 << NrSweepShift);

            for (int i = 0; i < steps; i++)
            {

                if (IsSubtraction)
                    result -= result / alteration;
                else
                    result += result / alteration;
            }
        }

        if (result >= 0x7FF)
        {
            if (IsSubtraction)
                result = 0;
            else
                OverflowListeners?.Invoke();
        }

        return result & 0x7FF;
    }

    private bool IsActive => SweepTime != 0 && NrSweepShift != 0;
    public ushort GetFrequencyDataChange(Address frequencyData, int times, byte sweepShifts, bool isSubtraction)
    {
        if (NrSweepShift == 0) return frequencyData;

        Address result = frequencyData;

        if (isSubtraction)
        {
            for (int i = 0; i < times; i++)
            {
                Address shift = result / (1 << sweepShifts);
                result -= shift;
            }
        }
        else
            for (int i = 0; i < times; i++)
            {
                Address shift = result / (1 << sweepShifts);
                result += shift;
            }

        if (result >= 0x7FF)
        {
            if (IsSubtraction)
                result = 0;
            else
                OverflowListeners?.Invoke();
        }

        return result;
    }

}