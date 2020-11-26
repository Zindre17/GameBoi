using static ByteOperations;

public class InterruptRegister : MaskedRegister
{
    public InterruptRegister() : base(0xE0) { }

    public bool Any() => data != mask;

    public bool Vblank
    {
        get => data[0];
        set => Write(value ? SetBit(0, data) : ResetBit(0, data));
    }

    public bool LcdStat
    {
        get => data[1];
        set => Write(value ? SetBit(1, data) : ResetBit(1, data));
    }
    public bool Timer
    {
        get => data[2];
        set => Write(value ? SetBit(2, data) : ResetBit(2, data));
    }
    public bool Serial
    {
        get => data[3];
        set => Write(value ? SetBit(3, data) : ResetBit(3, data));
    }
    public bool Joypad
    {
        get => data[4];
        set => Write(value ? SetBit(4, data) : ResetBit(4, data));
    }

}
