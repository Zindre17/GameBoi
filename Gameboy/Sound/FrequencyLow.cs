using GB_Emulator.Memory;

namespace GB_Emulator.Sound
{
    public class FrequencyLow : Register
    {
        public override Byte Read() => 0xFF;

        public Byte LowBits => data;

    }
}
