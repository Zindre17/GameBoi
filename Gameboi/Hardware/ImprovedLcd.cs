using System;
using System.Collections.Generic;
using System.Linq;
using Gameboi.Graphics;
using Gameboi.Memory.Io;
using Gameboi.Memory.Specials;
using static Gameboi.Hardware.LcdConstants;

namespace Gameboi.Hardware;

public class ImprovedLcd : IClocked
{
    private readonly SystemState state;

    public ImprovedLcd(SystemState state) => this.state = state;

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
        Array.Clear(spritePixels);

        LcdControl lcdControl = state.LcdControl;

        if (lcdControl.IsBackgroundEnabled)
        {
            ProcessBackgroundLine(lcdControl.BackgroundUsesHighTileMapArea, lcdControl.BackgroundAndWindowUsesLowTileDataArea);
            if (lcdControl.IsWindowEnabled)
            {
                ProcessWindowLine(lcdControl.WindowUsesHighTileMapArea, lcdControl.BackgroundAndWindowUsesLowTileDataArea);
            }
        }

        if (lcdControl.IsSpritesEnabled)
        {
            ProcessSpritesLine();
        }
    }

    private void ProcessBackgroundLine(bool useHighTileMapArea, bool useLowTileDataArea)
    {
        var scrollX = state.ScrollX;
        var scrollY = state.ScrollY;
        var spaceY = (byte)(scrollY + state.LineY);
        var spaceX = scrollX;

        var tileMapIndex = TileMap.GetTileMapIndex(spaceX, spaceY);

        var tileY = spaceY % TileSize;
        var tileX = scrollX % TileSize;

        var tile = GetTileData(useHighTileMapArea, useLowTileDataArea, tileMapIndex);

        for (var i = 0; i < ScreenWidth; i++)
        {
            backgroundAndWindowColors[i] = tile.GetColorIndex(tileY, tileX);

            if (++tileX is TileSize)
            {
                tileX = 0;
                spaceX += TileSize;
                tileMapIndex = TileMap.GetTileMapIndex(spaceX, spaceY);
                tile = GetTileData(useHighTileMapArea, useLowTileDataArea, tileMapIndex);
            }
        }
    }

    private void ProcessWindowLine(bool useHighTileMapArea, bool useLowTileDataArea)
    {
        if (state.LcdWindowTriggered is false)
        {
            return;
        }

        if (state.WindowX > 166)
        {
            state.LcdLinesOfWindowDrawnThisFrame++;
            return;
        }

        var tileX = 0;
        var tileY = state.LcdLinesOfWindowDrawnThisFrame % TileSize;
        var currentTileRow = state.LcdLinesOfWindowDrawnThisFrame / TileSize;
        var tileMapIndex = currentTileRow * TileMapSize;
        var tile = GetTileData(useHighTileMapArea, useLowTileDataArea, tileMapIndex);

        for (var i = state.WindowX - 7; i < ScreenWidth; i++)
        {
            if (i < 0)
            {
                continue;
            }

            backgroundAndWindowColors[i] = tile.GetColorIndex(tileY, tileX);

            if (++tileX is TileSize)
            {
                tileX = 0;
                tileMapIndex++;
                tile = GetTileData(useHighTileMapArea, useLowTileDataArea, tileMapIndex);
            }
        }

        state.LcdLinesOfWindowDrawnThisFrame++;
    }

    private ImprovedTile GetTileData(bool useHighTileMapArea, bool useLowTileDataArea, int tileMapIndex)
    {
        var tileDataIndex = TileMap.GetTileDataIndex(state.VideoRam, useHighTileMapArea, tileMapIndex);
        return ImprovedTileData.GetTileData(state.VideoRam, useLowTileDataArea, tileDataIndex);
    }

    private void ProcessSpritesLine()
    {
        LcdControl control = state.LcdControl;

        foreach (var (sprite, spriteTileRow) in spritesOnScanLine
            .OrderByDescending(pair => pair.Item1.X)
            .ThenByDescending(pair => pair.Item1.SpriteNr))
        {
            if (IsSpriteVisible(sprite) is false)
            {
                continue;
            }

            var tileData = ImprovedTileData.GetSpriteTileData(state.VideoRam, control.IsDoubleSpriteSize
                ? (byte)(sprite.TileIndex & 0xfe)
                : sprite.TileIndex);

            var tileRow = spriteTileRow;
            if (tileRow > 7)
            {
                tileRow -= 8;
                tileData = ImprovedTileData.GetSpriteTileData(state.VideoRam, (byte)(sprite.TileIndex | 1));
            }

            if (sprite.Yflip)
            {
                tileRow = 7 - tileRow;
            }

            ImprovedPalette palette = sprite.UsePalette1
                ? state.ObjectPalette1
                : state.ObjectPalette0;

            var screenStartIndex = sprite.X - TileSize;
            for (var tileColumn = 0; tileColumn < TileSize; tileColumn++)
            {
                var rowPixelIndex = screenStartIndex + tileColumn;

                if (IsOffScreen(rowPixelIndex))
                {
                    continue;
                }

                var column = sprite.Xflip ? 7 - tileColumn : tileColumn;
                var colorIndex = tileData.GetColorIndex(tileRow, column);

                if (colorIndex is 0)
                {
                    continue;
                }

                if (sprite.Hidden && backgroundAndWindowColors[rowPixelIndex] > 0)
                {
                    continue;
                }

                spritePixels[rowPixelIndex] = new(palette.DecodeColorIndex(colorIndex));
            }
        }

        bool IsSpriteVisible(ImprovedSprite sprite) => sprite.X is >= 0 and < ScreenWidth + 8;

        bool IsOffScreen(int screenIndex) => screenIndex is < 0 or >= ScreenWidth;
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
