class NR52 : MaskedRegister
{
    public NR52() : base(0x70) { }

    public bool IsAllOn => data[7];

    public bool IsSoundOn(byte channel)
    {
        if (channel > 3) throw new System.Exception();
        return data[channel];
    }

    public override void Write(Byte value)
    {
        base.Write(value | (data & 0x0F));
    }

    public void TurnAllOn() => data |= 0x80;

    public void TurnAllOff() => data &= 0x70;

    public void TurnOn(int channel) => data |= (1 << channel);
    public void TurnOff(int channel) => data &= ~(1 << channel);

}