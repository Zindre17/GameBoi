class OAM : IMemoryRange
{
    private Sprite[] sprites = new Sprite[40];

    public OAM()
    {
        for (int i = 0; i < 40; i++)
        {
            sprites[i] = new Sprite();
        }
    }


    public IMemory this[Address address] { get => sprites[address / 4][address % 4]; set { } }

    public Address Size => 40 * 4;

    public Byte Read(Address address) => sprites[address / 4].Read(address % 4);

    public void Write(Address address, Byte value) => sprites[address / 4].Write(address % 4, value);

}