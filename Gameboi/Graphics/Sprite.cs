
class Sprite : IMemoryRange
{
    private Register x = new Register();
    private Register y = new Register();
    private Register pattern = new Register();
    private Register flags = new MaskedRegister(0x0F);

    public Sprite(Byte x, Byte y, Byte pattern, Byte flags)
    {
        this.x = new Register(x);
        this.y = new Register(y);
        this.pattern = new Register(pattern);
        this.flags = new MaskedRegister(0x0F);
        this.flags.Write(flags);
    }

    public Sprite() { }

    public byte X => x.Read();
    public byte Y => y.Read();
    public byte Pattern => pattern.Read();
    public bool Hidden => flags.Read()[7]; // Other refer to it as "Priority" => 0: display on top, 1: hide under 1,2 and 3 of bg and
    public bool Yflip => flags.Read()[6];
    public bool Xflip => flags.Read()[5];
    public bool Palette => flags.Read()[4];

    public Address Size => 4;

    public IMemory this[Address address]
    {
        get
        {
            return (byte)address switch
            {
                0 => x,
                1 => y,
                2 => pattern,
                3 => flags,
                _ => throw new System.Exception("invalid address")
            };
        }
        set { }
    }

    public Byte Read(Address address) => this[address].Read();

    public void Write(Address address, Byte value) => this[address].Write(value);

}
