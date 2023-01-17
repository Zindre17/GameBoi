using Gameboi.Memory;
using Gameboi.Memory.Specials;
using static Gameboi.Statics.ScreenSizes;
using static Gameboi.Statics.TileDataConstants;

namespace Gameboi.Graphics;

public class VRAM : IMemoryRange
{
    private readonly BackgroundMap tileMap = new();
    private readonly TileData tileData0 = new();

    private readonly TileData tileData1 = new();

    private readonly TileData[] tileData;
    private readonly IMemoryRange[] backgroundMap;
    private readonly BackgroundAttributeMap backgroundAttributeMap = new();

    private bool isColorMode = false;
    private int BankSelect => BankSelectRegister.Read() & 1;

    public bool IsLocked { get; set; } = false;

    public IMemory BankSelectRegister { get; private set; } = new Register();

    public Address Size => 0x2000;

    private readonly IMemory scx, scy, wx, wy;
    private readonly LCDC lcdc;

    public VRAM(LCDC lcdc, IMemory scx, IMemory scy, IMemory wx, IMemory wy)
    {
        this.lcdc = lcdc;
        this.scx = scx;
        this.scy = scy;
        this.wx = wx;
        this.wy = wy;
        tileData = new[] { tileData0, tileData1 };
        backgroundMap = new IMemoryRange[] { tileMap, backgroundAttributeMap };
    }

    public void SetColorMode(bool on)
    {
        BankSelectRegister.Write(0);
        isColorMode = on;
    }

    public Byte[] GetBackgroundLine(Byte line)
    {
        Byte[] pixelLine = new Byte[pixelsPerLine];

        if (!lcdc.IsBackgroundEnabled) return pixelLine;

        Byte scrollY = scy.Read();

        Byte screenY = scrollY + line;

        Byte mapY = screenY / 8;
        Byte tileY = screenY % 8;

        Byte scrollX = scx.Read();

        int palletOffset = 0;
        int tileBank = 0;

        for (int i = 0; i < pixelsPerLine; i++)
        {
            Byte screenX = scrollX + i;

            Byte mapX = screenX / 8;

            Byte tileX = screenX % 8;

            if (isColorMode)
            {
                var attribute = backgroundAttributeMap.GetBackgroundAttributes(mapX, mapY, lcdc.BgMapSelect);

                if (attribute.IsHorizontallyFlipped)
                    tileX = 7 - tileX;

                if (attribute.IsVerticallyFlipped)
                    tileY = 7 - tileY;

                palletOffset = attribute.PalletNr * 4;
                tileBank = attribute.VramBankNr;
            }

            Byte patternIndex = tileMap.GetTilePatternIndex(mapX, mapY, lcdc.BgMapSelect);
            Tile tile = tileData[tileBank].LoadTilePattern(patternIndex, lcdc.BgWdDataSelect);

            pixelLine[i] = tile.GetColorCode(tileX, tileY) + palletOffset;
        }

        return pixelLine;
    }

    private Byte linesOfWindowDrawn = 0;
    private Byte windowY = 0;

    public Byte[] GetWindowLine(Byte line)
    {
        if (line == 0)
        {
            linesOfWindowDrawn = 0;
            windowY = wy.Read();
        }

        Byte[] pixelLine = new Byte[pixelsPerLine];

        if (!lcdc.IsWindowEnabled)
        {
            return pixelLine;
        }

        if (windowY > line || windowY > 143)
        {
            return pixelLine;
        }

        Byte windowXstart = wx.Read();
        if (windowXstart > 166)
        {
            return pixelLine;
        }

        Byte mapY = linesOfWindowDrawn / 8;
        Byte tileY = linesOfWindowDrawn % 8;

        int palletOffset = 0;
        int tileBank = 0;

        int currentX = 0;
        for (int i = windowXstart - 7; i < pixelsPerLine; i++)
        {
            if (i < 0)
            {
                currentX++;
                continue;
            }

            Byte mapX = currentX / 8;
            Byte tileX = currentX % 8;

            currentX++;

            if (isColorMode)
            {
                var attribute = backgroundAttributeMap.GetBackgroundAttributes(mapX, mapY, lcdc.WdMapSelect);

                if (attribute.IsHorizontallyFlipped)
                    tileX = 7 - tileX;

                if (attribute.IsVerticallyFlipped)
                    tileY = 7 - tileY;

                palletOffset = attribute.PalletNr * 4;
                tileBank = attribute.VramBankNr;
            }

            Byte patternIndex = tileMap.GetTilePatternIndex(mapX, mapY, lcdc.WdMapSelect);
            Tile tile = tileData[tileBank].LoadTilePattern(patternIndex, lcdc.BgWdDataSelect);

            pixelLine[i] = tile.GetColorCode(tileX, tileY) + palletOffset + 1;
        }

        linesOfWindowDrawn++;
        return pixelLine;
    }

    public Tile GetTile(Byte patternIndex, int ramBank, bool dataSelect)
    {
        return tileData[ramBank].LoadTilePattern(patternIndex, dataSelect);
    }

    private IMemoryRange GetMemoryArea(Address address, out Address relativeAddress)
    {

        if (address >= tileDataSize)
        {
            relativeAddress = address - tileDataSize;
            return backgroundMap[BankSelect];
        }
        relativeAddress = address;
        return tileData[BankSelect];
    }

    public Byte Read(Address address, bool isCpu = false)
    {
        if (isCpu && IsLocked) return 0xFF;
        return GetMemoryArea(address, out Address relAdr).Read(relAdr, isCpu);
    }

    public void Write(Address address, Byte value, bool isCpu = false)
    {
        if (isCpu && IsLocked) return;
        GetMemoryArea(address, out Address relAdr).Write(relAdr, value, isCpu);
    }


    public void Set(Address address, IMemory replacement)
    {
        GetMemoryArea(address, out Address relAdr).Set(relAdr, replacement);
    }

}

