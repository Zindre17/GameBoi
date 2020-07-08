using static ScreenRelatedAddresses;
using static TileDataConstants;

class TileDataMap : IMemoryRange
{

    private Tile[] tiles = new Tile[tileCount];

    public IMemory this[Address address] { get => data(address); set { } }
    private Tile tile(Address address) => tiles[address / bytesPerTile];
    private IMemory data(Address address) => tile(address)[address % bytesPerTile];

    public Address Size => tileCount * bytesPerTile;

    public TileDataMap()
    {
        for (int i = 0; i < tileCount; i++)
            tiles[i] = new Tile();
    }

    public Byte Read(Address address) => this[address].Read();

    public void Write(Address address, Byte value) => this[address].Write(value);

    //dataSelect => false: 0x8800 - 0x97FF | true: 0x8000 - 0x8FFF
    public Tile LoadTilePattern(Byte patternIndex, bool dataSelect)
    {
        int index = patternIndex;

        if (!dataSelect && !patternIndex[7])
            index += startIndexOfSecondTable;

        return tiles[index];
    }

}