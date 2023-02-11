using System.Collections.Generic;
using System.Linq;
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

    private const int TileSize = 8;
    private const int ScreenWidth = 160;
    private const int TileMapSize = 32;

    private readonly Rgb[] spritePixels = new Rgb[ScreenWidth];
    private readonly Rgb[] backgroundAndWindowPixels = new Rgb[ScreenWidth];

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

        var startingTileMapIndex = TileMap.GetTileMapIndex(scrollX, scrollY);

        var tileY = scrollY % TileSize;
        var tileX = scrollX % TileSize;
        var tileMapIndex = startingTileMapIndex;

        var tile = GetTileData(useHighTileMapArea, useLowTileDataArea, tileMapIndex);

        for (var i = 0; i < ScreenWidth; i++)
        {
            backgroundAndWindowPixels[i] = GetBackgroundTileColor(tile, tileX, tileY);

            if (++tileX is TileSize)
            {
                tileX = 0;
                tileMapIndex++;
                tile = GetTileData(useHighTileMapArea, useLowTileDataArea, tileMapIndex);
            }
        }
    }

    private int linesOfWindowDrawnThisFrame = 0;

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
        var tileY = linesOfWindowDrawnThisFrame % TileSize;
        var currentTileRow = linesOfWindowDrawnThisFrame / TileSize;
        var tileMapIndex = currentTileRow * TileMapSize;

        var tile = GetTileData(useHighTileMapArea, useLowTileDataArea, tileMapIndex);

        for (var i = state.WindowX - 7; i < ScreenWidth; i++)
        {
            if (i < 0)
            {
                continue;
            }

            backgroundAndWindowPixels[i] = GetWindowTileColor(tile, tileX, tileY);

            if (++tileX is TileSize)
            {
                tileX = 0;
                tileMapIndex++;
                tile = GetTileData(useHighTileMapArea, useLowTileDataArea, tileMapIndex);
            }
        }

        linesOfWindowDrawnThisFrame++;
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
                spritePixels[screenStartIndex + tileColumn] = palette.DecodeColorIndex(colorIndex);
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
