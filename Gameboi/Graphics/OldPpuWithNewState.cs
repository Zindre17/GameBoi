using Gameboi.Memory;
using Gameboi.Memory.Io;
using static Gameboi.Statics.ScreenSizes;

namespace Gameboi.Graphics;

public class OldPpuWithNewState
{
    private readonly OldOamWithNewState oam;
    private readonly OldVramWithNewState vram;
    private readonly VramDma vramDma = new();

    private readonly SystemState state;

    private bool isColorMode;
    public bool IsColorMode
    {
        get => isColorMode;
        set
        {
            vram.SetColorMode(value);
            oam.IsColorMode = value;
            isColorMode = value;
        }
    }

    public OldPpuWithNewState(SystemState state)
    {
        this.state = state;

        oam = new OldOamWithNewState(state);
        vram = new OldVramWithNewState(state);
    }

    public (byte[], byte[], byte[]) GetLineLayers(byte line)
    {
        var backgroundLine = vram.GetBackgroundLine(line);
        var windowLine = vram.GetWindowLine(line);
        var spriteLine = GetSpriteLine(line, state.LcdControl, backgroundLine);

        return (backgroundLine, windowLine, spriteLine);
    }

    public byte[] GetSpriteLine(byte line, LcdControl lcdc, byte[] backgroundLine)
    {
        var spriteLine = new byte[pixelsPerLine];

        if (!lcdc.IsSpritesEnabled)
            return spriteLine;

        var sprites = oam.GetSpritesOnLine(line, lcdc.IsDoubleSpriteSize);

        foreach (var sprite in sprites)
        {
            var spriteY = line - sprite.ScreenYstart;

            var pattern = sprite.Pattern;

            if (lcdc.IsDoubleSpriteSize)
            {
                spriteY = sprite.Yflip ? 15 - spriteY : spriteY;
                if (spriteY >= 8)
                {
                    pattern = (byte)(sprite.Pattern | 0x01);
                    spriteY -= 8;
                }
                else
                    pattern = (byte)(sprite.Pattern & 0xFE);
            }
            else
            {
                spriteY = sprite.Yflip ? 7 - spriteY : spriteY;
            }

            var tile = vram.GetTile(pattern, IsColorMode ? sprite.VramBank : 0, true);

            // color 1-3 obp0, color 5 - 7 obp1 (0 is transparent)
            byte colorOffset = (byte)(IsColorMode ? sprite.ColorPalette * 4 : sprite.Palette ? 4 : 0);

            for (byte x = 0; x < 8; x++)
            {
                var screenXint = sprite.ScreenXstart + x;
                if (screenXint >= 160) break; // outside on the right already. No need to check the rest of the sprite
                if (screenXint < 0) continue; // outside on the left, but should still check the rest of the sprite

                byte screenX = (byte)screenXint;

                if (sprite.Hidden && backgroundLine[screenX] % 4 > 0) continue; // background has priority

                byte spriteX = (byte)(sprite.Xflip ? 7 - x : x);

                byte color = tile.GetColorCode(spriteX, spriteY);
                if (color == 0) continue; // transparent

                spriteLine[screenX] = (byte)(colorOffset + color);
            }
        }
        return spriteLine;
    }

    public void AllowBlockTransfer() => vramDma.TransferIfActive();
}
