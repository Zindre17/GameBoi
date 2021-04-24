using static GB_Emulator.Statics.ByteOperations;

namespace GB_Emulator.Gameboi.Memory.Specials
{
    public class TAC : MaskedRegister
    {
        public TAC() : base(0xF8) { }

        public bool IsStarted => TestBit(2, data);

        public Byte TimerSpeed => data & 3;

    }
}