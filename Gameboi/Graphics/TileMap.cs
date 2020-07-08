using static TileMapConstants;

class TileMap : IMemoryRange
{
    private IMemory[] tilemaps = new IMemory[tileMapTotalSize];

    public IMemory this[Address address] { get => tilemaps[address]; set { } }

    public Address Size => tilemaps.Length;

    public TileMap()
    {
        for (int i = 0; i < tileMapTotalSize; i++)
            tilemaps[i] = new Register();
    }

    //mapSelect => false: 0x9800 - 0x9BFF | true: 0x9C00 - 0x9FFF
    public Byte GetTilePatternIndex(Byte x, Byte y, bool mapSelect)
    {
        int index = y * tileMapWidth + x;

        if (mapSelect)
            index += tileMapSize;

        return tilemaps[index].Read();
    }

    public Byte Read(Address address) => this[address].Read();

    public void Write(Address address, Byte value) => this[address].Write(value);

}