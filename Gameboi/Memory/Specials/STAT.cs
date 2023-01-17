using static Gameboi.Statics.ByteOperations;

namespace Gameboi.Memory.Specials;

public class STAT : MaskedRegister

{
    public STAT() : base(0x80) { }

    public bool IsCoincidenceInterruptEnabled => data[6];
    public bool IsOAMInterruptEnabled => data[5];
    public bool IsVblankInterruptEnabled => data[4];
    public bool IsHblankInterruptEnabled => data[3];
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

