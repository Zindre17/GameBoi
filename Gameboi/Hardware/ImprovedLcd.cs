using System.Collections.Generic;
using Gameboi.Graphics;
using Gameboi.Memory.Io;

namespace Gameboi.Hardware;

public class ImprovedLcd : IClocked
{
    private readonly SystemState state;

    public ImprovedLcd(SystemState state) => this.state = state;

    private int remainingTicksInMode = SearchingOamDurationInTicks;

    public void Tick()
    {
        LcdControl lcdControl = state.LcdControl;
        if (lcdControl.IsLcdEnabled is false)
        {
            return;
        }

        if (--remainingTicksInMode is not 0)
        {
            return;
        }

        LcdStatus lcdStatus = state.LcdStatus;
        switch (lcdStatus.Mode)
        {
            case SearchingOam:
                SearchOam();
                SetNextMode(lcdStatus, TransferringDataToLcd);
                break;
            case TransferringDataToLcd:
                GeneratePixelLine();
                SetNextMode(lcdStatus, HorizontalBlank);
                break;
            case HorizontalBlank:
                if (++state.LineY is VerticalBlankLineYStart)
                {
                    SetNextMode(lcdStatus, VerticalBlank);
                    break;
                }
                SetNextMode(lcdStatus, SearchingOam);
                break;
            case VerticalBlank:
                SetNextMode(lcdStatus, SearchingOam);
                break;
        }
    }

    private void SetNextMode(LcdStatus status, byte nextMode)
    {
        state.LcdStatus = status.WithMode(nextMode);
        remainingTicksInMode = modeDurations[nextMode];
    }

    private readonly List<ImprovedSprite> spritesOnScanLine = new();
    private void SearchOam()
    {
        spritesOnScanLine.Clear();

        foreach (var sprite in ImprovedOam.GetSprites(state.Oam))
        {
            if (!SpriteShowsOnScanLine(sprite))
            {
                continue;
            }

            spritesOnScanLine.Add(sprite);
            if (spritesOnScanLine.Count is 10)
            {
                break;
            }
        }
    }

    private const byte NormalSpriteHeight = 8;
    private const byte DoublelSpriteHeight = NormalSpriteHeight * 2;

    private bool SpriteShowsOnScanLine(ImprovedSprite sprite)
    {
        var spriteEnd = sprite.Y - NormalSpriteHeight;
        var spriteHeight = NormalSpriteHeight;

        LcdControl lcdControl = state.LcdControl;
        if (lcdControl.IsDoubleSpriteSize)
        {
            spriteEnd = sprite.Y;
            spriteHeight = DoublelSpriteHeight;
        }

        return spriteEnd <= state.LineY && (state.LineY - spriteEnd) < spriteHeight;
    }

    private void GeneratePixelLine()
    {
        // TODO generate pixels from background, window and sprites
    }

    private const byte VerticalBlankLineYStart = 144;

    private const byte SearchingOam = 2;
    private const byte TransferringDataToLcd = 3;
    private const byte HorizontalBlank = 0;
    private const byte VerticalBlank = 1;

    private const int SearchingOamDurationInTicks = 80;
    private const int GeneratePixelLineDurationInTicks = 172; // Minimum
    private const int HorizontalBlankDurationInTicks = 204; // Maximum
    private const int VerticalBlankDurationInTicks = ScanLineDurationInTicks * 10;
    private const int ScanLineDurationInTicks = 456;

    private static readonly int[] modeDurations = new[]{
        HorizontalBlankDurationInTicks,
        VerticalBlankDurationInTicks,
        SearchingOamDurationInTicks,
        GeneratePixelLineDurationInTicks
    };
}
