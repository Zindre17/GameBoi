class Envelope : Register
{
    private const byte MaxVolume = 0x0F;

    public Byte InitialVolume
    {
        get => (data & 0xF0) >> 4;
        set => data = (value << 4) | (data & 0x0F);
    }

    public Address GetVolume() => short.MaxValue / MaxVolume * InitialVolume / 2;

    public bool IsIncrease => data[3];
    public Byte LengthOfStep => data & 7;
}