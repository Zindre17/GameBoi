using Gameboi.Memory;
using Gameboi.Memory.Io;
using static Gameboi.Statics.ScreenSizes;

namespace Gameboi.Graphics;

public class OldVramWithNewState
{
    private readonly OldBackgroundMapWithNewState tileMap;
    private readonly OldTileDataWithNewState tileData0;

    private readonly OldTileDataWithNewState tileData1;

    private readonly OldTileDataWithNewState[] tileData;
    private readonly OldBackgroundAttributeMapWithNewState backgroundAttributeMap;

    private bool isColorMode = false;

    public IMemory BankSelectRegister { get; private set; } = new Register();

    private readonly SystemState state;

    public OldVramWithNewState(SystemState state)
    {
        this.state = state;
        tileData0 = new(state);
        tileData1 = new(state); // TODO fix for gbc: use other vram bank
        tileData = new[] { tileData0, tileData1 };
        tileMap = new(state);
        backgroundAttributeMap = new(state);
    }

    public void SetColorMode(bool on)
    {
        BankSelectRegister.Write(0);
        isColorMode = on;
    }

    public byte[] GetBackgroundLine(byte line)
    {
        byte[] pixelLine = new byte[pixelsPerLine];
        LcdControl lcdc = state.LcdControl;
        if (!lcdc.IsBackgroundEnabled) return pixelLine;

        byte scrollY = state.ScrollY;

        byte screenY = (byte)(scrollY + line);

        byte mapY = (byte)(screenY / 8);
        byte tileY = (byte)(screenY % 8);

        byte scrollX = state.ScrollX;

        int palletOffset = 0;
        int tileBank = 0;

        for (int i = 0; i < pixelsPerLine; i++)
        {
            byte screenX = (byte)(scrollX + i);

            byte mapX = (byte)(screenX / 8);

            byte tileX = (byte)(screenX % 8);

            if (isColorMode)
            {
                var attribute = backgroundAttributeMap.GetBackgroundAttributes(mapX, mapY, lcdc.BackgroundUsesHighTileMapArea);

                if (attribute.IsHorizontallyFlipped)
                    tileX = (byte)(7 - tileX);

                if (attribute.IsVerticallyFlipped)
                    tileY = (byte)(7 - tileY);

                palletOffset = attribute.PalletNr * 4;
                tileBank = attribute.VramBankNr;
            }

            byte patternIndex = tileMap.GetTilePatternIndex(mapX, mapY, lcdc.BackgroundUsesHighTileMapArea);
            var tile = tileData[tileBank].LoadTilePattern(patternIndex, lcdc.BackgroundAndWindowUsesLowTileDataArea);

            pixelLine[i] = (byte)(tile.GetColorCode(tileX, tileY) + palletOffset);
        }

        return pixelLine;
    }

    private byte linesOfWindowDrawn = 0;
    private byte windowY = 0;

    public byte[] GetWindowLine(byte line)
    {
        if (line == 0)
        {
            linesOfWindowDrawn = 0;
            windowY = state.WindowY;
        }

        byte[] pixelLine = new byte[pixelsPerLine];

        LcdControl lcdc = state.LcdControl;
        if (!lcdc.IsWindowEnabled)
        {
            return pixelLine;
        }

        if (windowY > line || windowY > 143)
        {
            return pixelLine;
        }

        byte windowXstart = state.WindowX;
        if (windowXstart > 166)
        {
            return pixelLine;
        }

        byte mapY = (byte)(linesOfWindowDrawn / 8);
        byte tileY = (byte)(linesOfWindowDrawn % 8);

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

            byte mapX = (byte)(currentX / 8);
            byte tileX = (byte)(currentX % 8);

            currentX++;

            if (isColorMode)
            {
                var attribute = backgroundAttributeMap.GetBackgroundAttributes(mapX, mapY, lcdc.WindowUsesHighTileMapArea);

                if (attribute.IsHorizontallyFlipped)
                    tileX = (byte)(7 - tileX);

                if (attribute.IsVerticallyFlipped)
                    tileY = (byte)(7 - tileY);

                palletOffset = attribute.PalletNr * 4;
                tileBank = attribute.VramBankNr;
            }

            byte patternIndex = tileMap.GetTilePatternIndex(mapX, mapY, lcdc.WindowUsesHighTileMapArea);
            var tile = tileData[tileBank].LoadTilePattern(patternIndex, lcdc.BackgroundAndWindowUsesLowTileDataArea);

            pixelLine[i] = (byte)(tile.GetColorCode(tileX, tileY) + palletOffset + 1);
        }

        linesOfWindowDrawn++;
        return pixelLine;
    }

    public OldTileWithNewState GetTile(byte patternIndex, int ramBank, bool dataSelect)
    {
        return tileData[ramBank].LoadTilePattern(patternIndex, dataSelect);
    }
}

