using System;
using System.Collections.Generic;
using Gameboi.Cartridges;
using Gameboi.Memory.Io;
using Gameboi.Memory.Specials;
using static Gameboi.Graphics.LcdConstants;

namespace Gameboi.Graphics;

public class Lcd
{
    private readonly SystemState state;
    private readonly VramDma vramDma;
    private readonly IRenderer renderer;

    public Lcd(SystemState state, VramDma vramDma)
    {
        this.state = state;
        this.vramDma = vramDma;
        var header = new GameHeader(state.CartridgeRom);
        renderer = header.IsColorGame
            ? new ColorRenderer(state)
            : new Renderer(state);
    }

    public Action<byte, Rgba[]>? OnLineReady;

    public void Tick()
    {
        LcdControl lcdControl = state.LcdControl;
        if (lcdControl.IsLcdEnabled is false)
        {
            return;
        }

        LcdStatus lcdStatus = state.LcdStatus;
        state.LcdStatus = lcdStatus.WithCoincidenceFlag(state.LineY == state.LineYCompare);
        lcdStatus = state.LcdStatus;

        if (lcdStatus.CoincidenceFlag && lcdStatus.IsCoincidenceInterruptEnabled)
        {
            if (!state.WasPreviousLcdInterruptLineHigh)
            {
                var interruptRequests = new InterruptState(state.InterruptFlags);
                state.InterruptFlags = interruptRequests.WithLcdStatusSet();
            }
            state.WasPreviousLcdInterruptLineHigh = true;
        }
        else
        {
            if (lcdStatus.Mode is TransferringDataToLcd)
            {
                state.WasPreviousLcdInterruptLineHigh = false;
            }
        }

        if (--state.LcdRemainingTicksInMode is not 0)
        {
            if (lcdStatus.Mode is VerticalBlank && (state.LcdRemainingTicksInMode % ScanLineDurationInTicks) is 0)
            {
                state.LineY++;
            }
            return;
        }

        switch (lcdStatus.Mode)
        {
            case SearchingOam:
                SearchOam();
                if (state.LineY == state.WindowY)
                {
                    state.LcdWindowTriggered = true;
                }
                state.LcdStatus = lcdStatus.WithMode(TransferringDataToLcd);
                state.LcdRemainingTicksInMode = modeDurations[TransferringDataToLcd];
                break;
            case TransferringDataToLcd:
                GeneratePixelLine();
                OnLineReady?.Invoke(state.LineY, GetPixelLine());
                TrySetLcdStatusInterrupt(lcdStatus.IsHblankInterruptEnabled);
                state.LcdStatus = lcdStatus.WithMode(HorizontalBlank);
                state.LcdRemainingTicksInMode = modeDurations[HorizontalBlank];
                if (state.IsVramDmaInProgress && state.VramDmaModeIsHblank)
                {
                    vramDma.TransferBlock();
                }
                break;
            case HorizontalBlank:
                if (++state.LineY is VerticalBlankLineYStart)
                {
                    var interruptFlags = new InterruptState(state.InterruptFlags);
                    state.InterruptFlags = interruptFlags.WithVerticalBlankSet();
                    TrySetLcdStatusInterrupt(lcdStatus.IsVblankInterruptEnabled);
                    state.LcdStatus = lcdStatus.WithMode(VerticalBlank);
                    state.LcdRemainingTicksInMode = modeDurations[VerticalBlank];
                    break;
                }
                TrySetLcdStatusInterrupt(lcdStatus.IsOAMInterruptEnabled);
                state.LcdStatus = lcdStatus.WithMode(SearchingOam);
                state.LcdRemainingTicksInMode = modeDurations[SearchingOam];
                break;
            case VerticalBlank:
                state.LcdLinesOfWindowDrawnThisFrame = 0;
                state.LcdWindowTriggered = false;
                state.LineY = 0;
                TrySetLcdStatusInterrupt(lcdStatus.IsOAMInterruptEnabled);
                state.LcdStatus = lcdStatus.WithMode(SearchingOam);
                state.LcdRemainingTicksInMode = modeDurations[SearchingOam];
                break;
        }
    }

    private void TrySetLcdStatusInterrupt(bool condition)
    {
        if (condition)
        {
            if (!state.WasPreviousLcdInterruptLineHigh)
            {
                var interruptRequests = new InterruptState(state.InterruptFlags);
                state.InterruptFlags = interruptRequests.WithLcdStatusSet();
            }
            state.WasPreviousLcdInterruptLineHigh = true;
        }
        else
        {
            state.WasPreviousLcdInterruptLineHigh = false;
        }
    }

    private readonly List<(Sprite, int)> spritesOnScanLine = new();
    private void SearchOam()
    {
        spritesOnScanLine.Clear();

        foreach (var sprite in Oam.GetSprites(state.Oam))
        {
            if (!SpriteShowsOnScanLine(sprite, out var spriteTileRow))
            {
                continue;
            }

            spritesOnScanLine.Add((sprite, spriteTileRow));
            if (spritesOnScanLine.Count is 10)
            {
                break;
            }
        }
    }

    private const byte NormalSpriteHeight = 8;
    private const byte DoublelSpriteHeight = NormalSpriteHeight * 2;

    private bool SpriteShowsOnScanLine(Sprite sprite, out int tileRow)
    {
        var spriteStart = sprite.Y - DoublelSpriteHeight;
        if (spriteStart > 143 || sprite.Y < 0)
        {
            tileRow = -1;
            return false;
        }

        LcdControl control = state.LcdControl;
        var spriteHeight = control.IsDoubleSpriteSize ? DoublelSpriteHeight : NormalSpriteHeight;
        tileRow = state.LineY - spriteStart;

        return tileRow >= 0 && tileRow < spriteHeight;
    }

    private const int ScreenWidth = 160;

    private readonly Rgba[] spritePixels = new Rgba[ScreenWidth];
    private readonly int[] backgroundAndWindowColors = new int[ScreenWidth];

    private Rgba[] GetPixelLine()
    {
        var result = new Rgba[ScreenWidth];
        var palette = new Palette(state.BackgroundPalette);

        for (var i = 0; i < ScreenWidth; i++)
        {
            if (spritePixels[i].Alpha is 0)
            {
                var color = palette.DecodeColorIndex(backgroundAndWindowColors[i]);
                result[i] = new(color);
                continue;
            }

            result[i] = spritePixels[i];
        }
        return result;
    }

    private void GeneratePixelLine()
    {
        Array.Copy(renderer.GeneratePixelLine(spritesOnScanLine), spritePixels, spritePixels.Length);
    }
}

public static class LcdConstants
{
    public const byte VerticalBlankLineYStart = 144;

    public const byte SearchingOam = 2;
    public const byte TransferringDataToLcd = 3;
    public const byte HorizontalBlank = 0;
    public const byte VerticalBlank = 1;

    public const int SearchingOamDurationInTicks = 80;
    public const int GeneratePixelLineDurationInTicks = 168; // Minimum
    public const int HorizontalBlankDurationInTicks = 208; // Maximum
    public const int VerticalBlankDurationInTicks = ScanLineDurationInTicks * 10;
    public const int ScanLineDurationInTicks = 456;

    public static readonly int[] modeDurations = new[]{
        HorizontalBlankDurationInTicks,
        VerticalBlankDurationInTicks,
        SearchingOamDurationInTicks,
        GeneratePixelLineDurationInTicks
    };
}
