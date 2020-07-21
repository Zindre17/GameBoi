class FrequencyHigh : MaskedRegister
{
    public FrequencyHigh() : base(0x38) { }

    public bool IsInitial
    {
        get => data[7];
        set => data = value ? (data | 0x80) : (data & 0x7F);
    }

    public bool HasDuration => data[6];

    public Byte HighBits
    {
        get => data & 7;
        set => base.Write(value | (data & 0xF8));
    }

    public override Byte Read() => data | 0xBF;

}