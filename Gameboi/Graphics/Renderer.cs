using System;
using System.Collections.Generic;
using System.Linq;
using Gameboi.Io;

namespace Gameboi.Graphics;

public interface IRenderer
{
    Rgba[] GeneratePixelLine(IEnumerable<(Sprite, int)> sprites);
}

public class Renderer : IRenderer
{
    private readonly SystemState state;

    public Renderer(SystemState state) => this.state = state;

    private const int TileSize = 8;
    private const int ScreenWidth = 160;
    private const int TileMapSize = 32;

    private readonly Rgba[] pixelLine = new Rgba[ScreenWidth];
    private readonly int[] backgroundAndWindowColors = new int[ScreenWidth];

    public Rgba[] GeneratePixelLine(IEnumerable<(Sprite, int)> spritesOnScanLine)
    {
        LcdControl lcdControl = state.LcdControl;

        Array.Clear(backgroundAndWindowColors);
        Array.Clear(pixelLine);

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
            ProcessSpritesLine(spritesOnScanLine);
        }

        var palette = new Palette(state.BackgroundPalette);

        for (var i = 0; i < ScreenWidth; i++)
        {
            if (pixelLine[i].Alpha is 0)
            {
                var color = palette.DecodeColorIndex(backgroundAndWindowColors[i]);
                pixelLine[i] = new(color);
                continue;
            }
        }

        return pixelLine;
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
                tileX++;
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

    private Tile GetTileData(bool useHighTileMapArea, bool useLowTileDataArea, int tileMapIndex)
    {
        var tileDataIndex = TileMap.GetTileDataIndex(state.VideoRam, useHighTileMapArea, tileMapIndex);
        return TileData.GetTileData(state.VideoRam, useLowTileDataArea, tileDataIndex);
    }

    private void ProcessSpritesLine(IEnumerable<(Sprite, int)> spritesOnScanLine)
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

            var tileData = TileData.GetSpriteTileData(state.VideoRam, control.IsDoubleSpriteSize
                ? (byte)(sprite.TileIndex & 0xfe)
                : sprite.TileIndex);

            var tileRow = spriteTileRow;
            if (tileRow > 7)
            {
                tileRow -= 8;
                tileData = TileData.GetSpriteTileData(state.VideoRam, (byte)(sprite.TileIndex | 1));
            }

            if (sprite.Yflip)
            {
                tileRow = 7 - tileRow;
            }

            Palette palette = sprite.UsePalette1
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

                pixelLine[rowPixelIndex] = new(palette.DecodeColorIndex(colorIndex));
            }
        }

        bool IsSpriteVisible(Sprite sprite) => sprite.X is >= 0 and < ScreenWidth + 8;

        bool IsOffScreen(int screenIndex) => screenIndex is < 0 or >= ScreenWidth;
    }
}

