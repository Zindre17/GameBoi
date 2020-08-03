class Envelope : Register
{
    public const byte MaxVolume = 0x0F;

    public Byte InitialVolume
    {
        get => (data & 0xF0) >> 4;
        set => data = (value << 4) | (data & 0x0F);
    }

    public Address GetVolume() => GetVolume(InitialVolume);
    public Address GetVolume(byte volume) => 5 * volume;

    public bool IsIncrease => data[3];
    public Byte LengthOfStep => data & 7;
}