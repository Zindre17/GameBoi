using static ByteOperations;

class InterruptRegister : MaskedRegister
{
    public InterruptRegister() : base(0xE0) { }

    public bool Any() => (data & 0x1F) != 0;

    public bool Vblank
    {
        get => TestBit(0, data);
        set => data = value ? SetBit(0, data) : ResetBit(0, data);
    }

    public bool LcdStat
    {
        get => TestBit(1, data);
        set => data = value ? SetBit(1, data) : ResetBit(1, data);
    }
    public bool Timer
    {
        get => TestBit(2, data);
        set => data = value ? SetBit(2, data) : ResetBit(2, data);
    }
    public bool Serial
    {
        get => TestBit(3, data);
        set => data = value ? SetBit(3, data) : ResetBit(3, data);
    }
    public bool Joypad
    {
        get => TestBit(4, data);
        set => data = value ? SetBit(4, data) : ResetBit(4, data);
    }

}
