using GB_Emulator.Gameboi.Memory;

namespace GB_Emulator.Sound
{
    public class WaveDuty : Register
    {
        private Byte Duty => data >> 6;
        private Byte SoundLength => data & 0x3F;

        public override Byte Read() => data | 0x3F;

        private const double soundLengthDenominator = 1 / 256d;

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
    }
}