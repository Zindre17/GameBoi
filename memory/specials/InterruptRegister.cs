using static ByteOperations;

class InterruptRegister : MaskedRegister
{
    public InterruptRegister() : base(0xE0) { }

    public bool Vblank => TestBit(0, data);
    public bool LcdStat => TestBit(1, data);
    public bool Timer => TestBit(2, data);
    public bool Serial => TestBit(3, data);
    public bool Joypad => TestBit(4, data);

}
