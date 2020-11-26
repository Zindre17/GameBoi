using static ScreenSizes;
using static TileMapConstants;
using static TileDataConstants;
using System;

public class VRAM : IMemoryRange, ILockable
{
    private bool isLocked = false;

    private TileMap tileMap = new TileMap();
    private TileDataMap tileDataMap = new TileDataMap();

    public Address Size => 0x2000;

    private IMemory scx, scy, wx, wy;
    private LCDC lcdc;

    public VRAM(LCDC lcdc, IMemory scx, IMemory scy, IMemory wx, IMemory wy)
    {
        this.lcdc = lcdc;
        this.scx = scx;
        this.scy = scy;
        this.wx = wx;
        this.wy = wy;
    }

    private Byte linesOfWindowDrawn = 0;
    public Byte[] GetBackgroundAndWindowLine(Byte line)
    {
        if (line == 0) linesOfWindowDrawn = 0;

        Byte[] pixelLine = new Byte[pixelsPerLine];
        Byte scrollY = scy.Read();

        Byte screenY = scrollY + line;

        Byte mapY = screenY / 8;
        Byte tileY = screenY % 8;

        Byte scrollX = scx.Read();

        if (lcdc.IsBackgroundEnabled)
        {
            for (int i = 0; i < pixelsPerLine; i++)
            {
                Byte screenX = scrollX + i;

                Byte mapX = screenX / 8;
                Byte patternIndex = tileMap.GetTilePatternIndex(mapX, mapY, lcdc.BgMapSelect);

                Tile tile = tileDataMap.LoadTilePattern(patternIndex, lcdc.BgWdDataSelect);

                Byte tileX = screenX % 8;
                pixelLine[i] = tile.GetColorCode(tileX, tileY);
            }
        }
        if (lcdc.IsWindowEnabled)
        {
            Byte[] windowLine = GetWindowLine(line);
            for (int i = 0; i < pixelsPerLine; i++)
                if (windowLine[i] != 0) pixelLine[i] = windowLine[i] - 1;
            linesOfWindowDrawn++;
        }

        return pixelLine;
    }

    public Byte[] GetWindowLine(Byte line)
    {
        Byte[] pixelLine = new Byte[pixelsPerLine];


        Byte windowY = wy.Read();
        if (windowY > line) return pixelLine;

        windowY = line - wy.Read();
        if (windowY > 143) return pixelLine;
        Byte lag = windowY - linesOfWindowDrawn;
        windowY -= lag;

        Byte windowXstart = wx.Read();
        if (windowXstart > 166) return pixelLine;

        windowXstart -= 7;

        Byte windowXend;

        if (windowXstart[7])
        {
            windowXend = windowXstart + pixelsPerLine;
            windowXstart = 0;
        }
        else windowXend = pixelsPerLine;

        Byte mapY = windowY / 8;
        Byte tileY = windowY % 8;

        for (int i = windowXstart; i < windowXend; i++)
        {
            Byte mapX = (i - windowXstart) / 8;
            Byte patternIndex = tileMap.GetTilePatternIndex(mapX, mapY, lcdc.WdMapSelect);

            Tile tile = tileDataMap.LoadTilePattern(patternIndex, lcdc.BgWdDataSelect);

            Byte tileX = (i - windowXstart) % 8;
            pixelLine[i] = tile.GetColorCode(tileX, tileY) + 1;
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