using static ByteOperations;

public class TAC : MaskedRegister
{
    public TAC() : base(0xF8) { }

    public bool IsStarted => TestBit(2, data);

    public Byte TimerSpeed => data & 3;

}