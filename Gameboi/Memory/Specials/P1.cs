using static ByteOperations;

class P1 : MaskedRegister
{
    public P1() : base(0xC0) { Write(0x0F); }

    public bool P15 => !TestBit(5, data);
    public bool P14 => !TestBit(4, data);

}