using System;
using System.Collections.Generic;
using System.Linq;
using Gameboi.Graphics;
using Gameboi.Memory.Io;
using Gameboi.Memory.Specials;

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
        }
        else if ((nextMode is HorizontalBlank && status.IsHblankInterruptEnabled)
            || (nextMode is SearchingOam && status.IsOAMInterruptEnabled)
            )
        {
            state.InterruptFlags = interruptRequests.WithLcdStatusSet();
        }
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

    private const int TileSize = 8;
    private const int ScreenWidth = 160;
    private const int TileMapSize = 32;

    private readonly Rgba[] spritePixels = new Rgba[ScreenWidth];
    private readonly Rgba[] backgroundAndWindowPixels = new Rgba[ScreenWidth];


    private Rgba[] GetPixelLine()
    {
        var result = new Rgba[ScreenWidth];
        for (var i = 0; i < ScreenWidth; i++)
        {
            if (spritePixels[i].Alpha is 0)
            {
                result[i] = backgroundAndWindowPixels[i];
                continue;
            }

            result[i] = spritePixels[i];
        }
        return result;
    }

    private void GeneratePixelLine()
    {
        LcdControl lcdControl = state.LcdControl;

        if (lcdControl.IsBackgroundEnabled)
        {
            ProcessBackgroundLine(lcdControl.BackgroundUsesHighTileMapArea, lcdControl.BackgroundAndWindowUsesLowTileDataArea);
        }

        if (lcdControl.IsWindowEnabled)
        {
            ProcessWindowLine(lcdControl.WindowUsesHighTileMapArea, lcdControl.BackgroundAndWindowUsesLowTileDataArea);
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

        var tileMapIndex = TileMap.GetTileMapIndex(scrollX, scrollY);

        LcdControl lcdControl = state.LcdControl;

        var tileY = scrollY % TileSize;
        var tileX = scrollX % TileSize;

        var tileDataIndex = TileMap.GetTileDataIndex(state.VideoRam, lcdControl.BackgroundUsesHighTileMapArea, tileMapIndex);

        var tile = GetTileData(useHighTileMapArea, useLowTileDataArea, tileDataIndex);

        for (var i = 0; i < ScreenWidth; i++)
        {
            backgroundAndWindowPixels[i] = new(GetBackgroundTileColor(tile, tileX, tileY));

            if (++tileX is TileSize)
            {
                tileX = 0;
                tileMapIndex++;
                tileDataIndex = TileMap.GetTileDataIndex(state.VideoRam, lcdControl.BackgroundUsesHighTileMapArea, tileMapIndex);
                tile = GetTileData(useHighTileMapArea, useLowTileDataArea, tileDataIndex);
            }
        }
    }

    private void ProcessWindowLine(bool useHighTileMapArea, bool useLowTileDataArea)
    {
        if (state.LineY < state.WindowY)
        {
            return;
        }
        if (state.WindowX > 166)
        {
            return;
        }

        var tileX = 0;
        var tileY = state.LcdLinesOfWindowDrawnThisFrame % TileSize;
        var currentTileRow = state.LcdLinesOfWindowDrawnThisFrame / TileSize;
        var tileMapIndex = currentTileRow * TileMapSize;
        LcdControl lcdControl = state.LcdControl;
        var tileDataIndex = TileMap.GetTileDataIndex(state.VideoRam, lcdControl.WindowUsesHighTileMapArea, tileMapIndex);
        var tile = GetTileData(useHighTileMapArea, useLowTileDataArea, tileDataIndex);

        for (var i = state.WindowX - 7; i < ScreenWidth; i++)
        {
            if (i < 0)
            {
                continue;
            }

            backgroundAndWindowPixels[i] = new(GetWindowTileColor(tile, tileX, tileY));

            if (++tileX is TileSize)
            {
                tileX = 0;
                tileMapIndex++;
                tileDataIndex = TileMap.GetTileDataIndex(state.VideoRam, lcdControl.WindowUsesHighTileMapArea, tileMapIndex);
                tile = GetTileData(useHighTileMapArea, useLowTileDataArea, tileDataIndex);
            }
        }

        state.LcdLinesOfWindowDrawnThisFrame++;
    }

    private ImprovedTile GetTileData(bool useHighTileMapArea, bool useLowTileDataArea, int tileMapIndex)
    {
        var tileDataIndex = TileMap.GetTileDataIndex(state.VideoRam, useHighTileMapArea, tileMapIndex);
        return ImprovedTileData.GetTileData(state.VideoRam, useLowTileDataArea, tileDataIndex);
    }

    private Rgb GetWindowTileColor(ImprovedTile tile, int x, int y)
        => GetBackgroundTileColor(tile, x, y);

    private Rgb GetBackgroundTileColor(ImprovedTile tile, int x, int y)
    {
        var colorIndex = tile.GetColorIndex(x, y);
        ImprovedPalette palette = state.BackgroundPalette;
        return palette.DecodeColorIndex(colorIndex);
    }

    private void ProcessSpritesLine()
    {
        foreach (var sprite in spritesOnScanLine
            .OrderByDescending(sprite => sprite.X)
            .ThenByDescending(sprite => sprite.SpriteNr))
        {
            if (IsSpriteVisible(sprite) is false)
            {
                continue;
            }

            var tileData = ImprovedTileData.GetSpriteTileData(state.VideoRam, sprite.TileIndex);
            var tileRow = state.LineY - sprite.Y;

            ImprovedPalette palette = sprite.UsePalette1
                ? state.ObjectPalette1
                : state.ObjectPalette0;

            var screenStartIndex = sprite.X - TileSize;
            for (var tileColumn = 0; tileColumn < TileSize; tileColumn++)
            {
                if (IsOffScreen(screenStartIndex + tileColumn))
                {
                    continue;
                }

                var colorIndex = tileData.GetColorIndex(tileRow, tileColumn);
                spritePixels[screenStartIndex + tileColumn] = new(palette.DecodeColorIndex(colorIndex));
            }
        }

        bool IsSpriteVisible(ImprovedSprite sprite) => sprite.X is 0 or >= ScreenWidth + 8;

        bool IsOffScreen(int screenIndex) => screenIndex is < 0 or >= ScreenWidth;
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
