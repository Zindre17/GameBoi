namespace Gameboi.Sound;

public readonly struct WaveDuty
{
    private readonly byte data;

    public WaveDuty(byte data) => this.data = data;

    private int Duty => data >> 6;
    private int SoundLength => data & 0x3F;

    public double GetSoundLengthInSeconds() => (64 - SoundLength) * soundLengthDenominator;

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

    private const double soundLengthDenominator = 1 / 256d;
}
