class Sweep : MaskedRegister
{
    public Sweep() : base(0x80) { }

    public Byte SweepTime => (data & 0x70) >> 4;
    public bool IsSubtraction => data[3];
    public Byte NrSweepShift => data & 7;

    public delegate void OnSweepOverflow();
    public OnSweepOverflow OverflowListeners;

    public ushort GetFrequencyDataChange(Address frequencyData, int times)
    {
        if (NrSweepShift == 0) return frequencyData;

        Address result = frequencyData;

        if (IsSubtraction)
        {
            for (int i = 0; i < times; i++)
            {
                Address shift = result / (1 << NrSweepShift);
                result -= shift;
            }
        }
        else
            for (int i = 0; i < times; i++)
            {
                Address shift = result / (1 << NrSweepShift);
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