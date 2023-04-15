using System;
using System.Collections.Generic;
using Gameboi.Cartridges;
using Gameboi.Graphics;
using Gameboi.Memory.Io;
using Gameboi.Memory.Specials;
using static Gameboi.Hardware.LcdConstants;

namespace Gameboi.Hardware;

public class ImprovedLcd : IClocked
{
    private readonly SystemState state;
    private readonly IRenderer renderer;

    public ImprovedLcd(SystemState state)
    {
        this.state = state;
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
        if (lcdStatus.CoincidenceFlag && lcdStatus.IsCoincidenceInterruptEnabled)
        {
            var interruptRequests = new InterruptState(state.InterruptFlags);
            state.InterruptFlags = interruptRequests.WithLcdStatusSet();
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
                SetNextMode(lcdStatus, TransferringDataToLcd);
                break;
            case TransferringDataToLcd:
                GeneratePixelLine();
                OnLineReady?.Invoke(state.LineY, GetPixelLine());
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
                state.LcdLinesOfWindowDrawnThisFrame = 0;
                state.LcdWindowTriggered = false;
                state.LineY = 0;
                break;
        }
    }

    private void SetNextMode(LcdStatus status, byte nextMode)
    {
        state.LcdStatus = status.WithMode(nextMode);
        state.LcdRemainingTicksInMode = modeDurations[nextMode];

        var interruptRequests = new InterruptState(state.InterruptFlags);
        if (nextMode is VerticalBlank)
        {
            state.InterruptFlags = interruptRequests.WithVerticalBlankSet();
            if (status.IsVblankInterruptEnabled)
            {
                state.InterruptFlags = interruptRequests.WithLcdStatusSet();
            }
        }
        else if ((nextMode is HorizontalBlank && status.IsHblankInterruptEnabled)
            || (nextMode is SearchingOam && status.IsOAMInterruptEnabled)
            )
        {
            state.InterruptFlags = interruptRequests.WithLcdStatusSet();
        }
    }

    private readonly List<(ImprovedSprite, int)> spritesOnScanLine = new();
    private void SearchOam()
    {
        spritesOnScanLine.Clear();

        foreach (var sprite in ImprovedOam.GetSprites(state.Oam))
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

    private bool SpriteShowsOnScanLine(ImprovedSprite sprite, out int tileRow)
    {
        var spriteStart = sprite.Y - DoublelSpriteHeight;
        if (spriteStart is < 0 or > 143)
        {
            tileRow = -1;
            return false;
        }

        LcdControl control = state.LcdControl;
        var spriteHeight = control.IsDoubleSpriteSize ? DoublelSpriteHeight : NormalSpriteHeight;
        tileRow = state.LineY - spriteStart;

        return tileRow >= 0 && tileRow < spriteHeight;
    }

    private const int TileSize = 8;
    private const int ScreenWidth = 160;
    private const int TileMapSize = 32;

    private readonly Rgba[] spritePixels = new Rgba[ScreenWidth];
    private readonly int[] backgroundAndWindowColors = new int[ScreenWidth];

    private Rgba[] GetPixelLine()
    {
        var result = new Rgba[ScreenWidth];
        var palette = new ImprovedPalette(state.BackgroundPalette);

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
    public const int GeneratePixelLineDurationInTicks = 172; // Minimum
    public const int HorizontalBlankDurationInTicks = 204; // Maximum
    public const int VerticalBlankDurationInTicks = ScanLineDurationInTicks * 10;
    public const int ScanLineDurationInTicks = 456;

    public static readonly int[] modeDurations = new[]{
        HorizontalBlankDurationInTicks,
        VerticalBlankDurationInTicks,
        SearchingOamDurationInTicks,
        GeneratePixelLineDurationInTicks
    };
}
