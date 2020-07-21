class WaveDuty : Register
{
    public Byte Duty => (data & 0xC0) >> 6;
    public Byte SoundLength => data & 0x3F;

    public override Byte Read() => data | 0x3F;

    private const double soundLengthDenominator = 1 / 256d;
    public double GetSoundLengthInMs() => (64 - SoundLength) * soundLengthDenominator * 1000;

    public double GetDuty()
    {
        return (byte)Duty switch
        {
            0 => 0.125,
            1 => 0.25,
            2 => 0.5,
            3 => 0.75,
            _ => throw new System.Exception()
        };

    }
}