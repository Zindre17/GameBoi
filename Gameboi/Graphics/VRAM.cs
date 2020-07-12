using static ScreenSizes;
using static TileMapConstants;
using static TileDataConstants;

class VRAM : IMemoryRange, ILockable
{
    private bool isLocked = false;

    private TileMap tileMap = new TileMap();
    private TileDataMap tileDataMap = new TileDataMap();

    public Address Size => 0x2000;

    private IMemory scx, scy;
    private LCDC lcdc;

    public VRAM(IMemory scx, IMemory scy, LCDC lcdc) => (this.scx, this.scy, this.lcdc) = (scx, scy, lcdc);

    public Byte[] GetBackgroundLine(Byte line)
    {
        Byte[] pixelLine = new Byte[pixelsPerLine];
        Byte scrollY = scy.Read();

        Byte screenY = scrollY + line;

        Byte mapY = screenY / 8;
        Byte tileY = screenY % 8;

        Byte scrollX = scx.Read();
        for (int i = 0; i < pixelsPerLine; i++)
        {
            Byte screenX = scrollX + i;

            Byte mapX = screenX / 8;
            Byte patternIndex = tileMap.GetTilePatternIndex(mapX, mapY, lcdc.BgMapSelect);

            Tile tile = tileDataMap.LoadTilePattern(patternIndex, lcdc.BgWdDataSelect);

            Byte tileX = screenX % 8;
            pixelLine[i] = tile.GetColorCode(tileX, tileY);
        }

        return pixelLine;
    }

    public Tile GetTile(Byte patternIndex, bool dataSelect)
    {
        return tileDataMap.LoadTilePattern(patternIndex, dataSelect);
    }

    private IMemoryRange GetMemoryArea(Address address, out Address relativeAddress)
    {

        if (address >= tileDataSize)
        {
            relativeAddress = address - tileDataSize;
            return tileMap;
        }
        relativeAddress = address;
        return tileDataMap;
    }

    public Byte Read(Address address, bool isCpu = false) => GetMemoryArea(address, out Address relAdr).Read(relAdr, isCpu);

    public void Write(Address address, Byte value, bool isCpu = false) => GetMemoryArea(address, out Address relAdr).Write(relAdr, value, isCpu);

    public void Set(Address address, IMemory replacement) => GetMemoryArea(address, out Address relAdr).Set(relAdr, replacement);

    public void SetLock(bool on) => isLocked = on;

}