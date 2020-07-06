class MaskedRegister : Register
{
    protected Byte mask;

    public MaskedRegister(Byte mask, bool isReadOnly = false) : base(mask, isReadOnly) => this.mask = mask;

    public override void Write(Byte value) => base.Write(value | mask);

}