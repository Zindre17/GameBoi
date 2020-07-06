using static ByteOperations;

class STAT : MaskedRegister

{
    public STAT() : base(0x80) { }

    public bool IsCoincidenceInterruptEnabled { get => TestBit(6, data); set => Write(value ? SetBit(6, data) : ResetBit(6, data)); }
    public bool IsOAMInterruptEnabled { get => TestBit(5, data); set => Write(value ? SetBit(5, data) : ResetBit(5, data)); }
    public bool IsVblankInterruptEnabled { get => TestBit(4, data); set => Write(value ? SetBit(4, data) : ResetBit(4, data)); }
    public bool IsHblankInterruptEnabled { get => TestBit(3, data); set => Write(value ? SetBit(3, data) : ResetBit(3, data)); }
    public bool CoincidenceFlag { get => TestBit(2, data); set => base.Write(value ? SetBit(2, data) : ResetBit(2, data)); }

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