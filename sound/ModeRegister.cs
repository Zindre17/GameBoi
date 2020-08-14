class ModeRegister : MaskedRegister
{
    public ModeRegister(byte mask = 0x3F)
    {
        this.mask = mask;
        data = mask;
    }

    public bool IsInitial
    {
        get => data[7];
        set => data = value ? (data | 0x80) : (data & 0x7F);
    }

    public bool HasDuration => data[6];

    public override Byte Read() => data | 0xBF;
}