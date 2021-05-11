using GB_Emulator.Gameboi.Memory;
using GB_Emulator.Gameboi.Memory.Specials;
using static GB_Emulator.Statics.ScreenSizes;
using static GB_Emulator.Statics.TileDataConstants;

namespace GB_Emulator.Gameboi.Graphics
{
    public class VRAM : IMemoryRange
    {
        private readonly TileMap tileMap = new();
        private readonly TileDataMap tileDataMap = new();

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

        private Byte linesOfWindowDrawn = 0;

        public Byte[] GetWindowLine(Byte line)
        {
            if (line == 0) linesOfWindowDrawn = 0;

            Byte[] pixelLine = new Byte[pixelsPerLine];

            if (!lcdc.IsWindowEnabled)
            {
                return pixelLine;
            }

            Byte windowY = wy.Read();
            if (windowY > line)
            {
                return pixelLine;
            }

            windowY = linesOfWindowDrawn;
            if (windowY > 143)
            {
                return pixelLine;
            }
            linesOfWindowDrawn++;

            Byte windowXstart = wx.Read();
            if (windowXstart > 166)
            {
                return pixelLine;
            }

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

        public Byte Read(Address address, bool isCpu = false)
        {
            return GetMemoryArea(address, out Address relAdr).Read(relAdr, isCpu);
        }

        public void Write(Address address, Byte value, bool isCpu = false)
        {
            GetMemoryArea(address, out Address relAdr).Write(relAdr, value, isCpu);
        }


        public void Set(Address address, IMemory replacement)
        {
            GetMemoryArea(address, out Address relAdr).Set(relAdr, replacement);
        }

    }
}