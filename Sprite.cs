using static ByteOperations;

class Sprite
{
    private byte x;
    private byte y;
    private byte pattern;
    private byte flags;

    public Sprite(byte x, byte y, byte pattern, byte flags)
    {
        this.x = x;
        this.y = y;
        this.pattern = pattern;
        this.flags = flags;
    }

    public byte X => x;
    public byte Y => y;
    public byte Pattern => pattern;
    public bool Hidden => TestBit(7, flags); // Other refer to it as "Priority" => 0: display on top, 1: hide under 1,2 and 3 of bg and
    public bool Yflip => TestBit(6, flags);
    public bool Xflip => TestBit(5, flags);
    public bool Pallet1 => TestBit(4, flags);
}