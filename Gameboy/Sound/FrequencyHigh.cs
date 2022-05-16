using GB_Emulator.Gameboi.Memory;

namespace GB_Emulator.Sound
{
    public class FrequencyHigh : ModeRegister
    {
        public FrequencyHigh() : base(0x38) { }

        public Byte HighBits => data & 7;

    }
}