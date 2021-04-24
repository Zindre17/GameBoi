using static GB_Emulator.Statics.ByteOperations;

namespace GB_Emulator.Gameboi.Memory.Specials
{
    public class STAT : MaskedRegister

    {
        public STAT() : base(0x80) { }

        public bool IsCoincidenceInterruptEnabled { get => data[6]; set => Write(value ? SetBit(6, data) : ResetBit(6, data)); }
        public bool IsOAMInterruptEnabled { get => data[5]; set => Write(value ? SetBit(5, data) : ResetBit(5, data)); }
        public bool IsVblankInterruptEnabled { get => data[4]; set => Write(value ? SetBit(4, data) : ResetBit(4, data)); }
        public bool IsHblankInterruptEnabled { get => data[3]; set => Write(value ? SetBit(3, data) : ResetBit(3, data)); }
        public bool CoincidenceFlag { get => data[2]; set => base.Write(value ? SetBit(2, data) : ResetBit(2, data)); }

        public override void Write(Byte value) => base.Write(value & 0xF8 | data & 0x07);

        public Byte Mode
        {
            get => data & 3;
            set
            {
                Byte exclMode = data & 0xFC;
                Byte sanitizedMode = value & 3;
                base.Write(exclMode | sanitizedMode);
            }
        }
    }
}