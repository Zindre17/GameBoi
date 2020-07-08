using static ScreenSizes;
using static TileMapConstants;
using static TileDataConstants;

class VRAM : IMemoryRange
{
    private TileMap tileMap = new TileMap();
    private TileDataMap tileDataMap = new TileDataMap();

    public IMemory this[Address address]
    {
        get
        {
            if (address >= tileDataSize) return tileMap[address - tileDataSize];
            return tileDataMap[address];
        }
        set { }
    }

    public Address Size => 0x2000;

    public byte[] GetLine(Byte line, Byte scrollX, Byte scrollY, bool mapSelect, bool dataSelect)
    {
        byte[] pixelLine = new byte[pixelsPerLine];

        Byte screenY = scrollY + line;

        Byte mapY = screenY / 8;
        Byte tileY = screenY % 8;

        for (int i = 0; i < pixelsPerLine; i++)
        {
            Byte screenX = scrollX + i;

            Byte mapX = screenX / 8;
            Byte patternIndex = tileMap.GetTilePatternIndex(mapX, mapY, mapSelect);

            Tile tile = tileDataMap.LoadTilePattern(patternIndex, dataSelect);

            Byte tileX = screenX % 8;
            pixelLine[i] = tile.GetColorCode(tileX, tileY);
        }

        return pixelLine;
    }

    public Tile GetTile(Byte patternIndex, bool dataSelect)
    {
        return tileDataMap.LoadTilePattern(patternIndex, dataSelect);
    }

    public Byte Read(Address address) => this[address].Read();

    public void Write(Address address, Byte value) => this[address].Write(value);
}