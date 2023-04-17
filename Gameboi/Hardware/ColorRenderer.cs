using System;
using System.Collections.Generic;
using System.Linq;
using Gameboi.Graphics;
using Gameboi.Memory.Io;

namespace Gameboi.Hardware;

public class ColorRenderer : IRenderer
{
    private readonly SystemState state;

    public ColorRenderer(SystemState state) => this.state = state;

    private const int TileSize = 8;
    private const int ScreenWidth = 160;
    private const int TileMapSize = 32;

    private readonly Rgba[] pixelLine = new Rgba[ScreenWidth];
    private readonly (int, int)[] backgroundAndWindowColors = new (int, int)[ScreenWidth];

    public Rgba[] GeneratePixelLine(IEnumerable<(ImprovedSprite, int)> spritesOnScanLine)
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

        var palette = new ImprovedColorPalette(state.BackgroundColorPaletteData);

        for (var i = 0; i < ScreenWidth; i++)
        {
            if (pixelLine[i].Alpha is 0)
            {
                var (colorIndex, palletNr) = backgroundAndWindowColors[i];
                var color = palette.DecodeColorIndex(palletNr, colorIndex);
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

        var (tile, attributes) = GetTileData(useHighTileMapArea, useLowTileDataArea, tileMapIndex);

        if (attributes.IsVerticallyFlipped)
        {
            tileY = 7 - tileY;
        }

        for (var i = 0; i < ScreenWidth; i++)
        {
            var x = tileX;
            if (attributes.IsHorizontallyFlipped)
            {
                x = 7 - x;
            }
            backgroundAndWindowColors[i] = (tile.GetColorIndex(tileY, x), attributes.PalletNr);

            if (++tileX is TileSize)
            {
                tileX = 0;
                spaceX += TileSize;
                tileMapIndex = TileMap.GetTileMapIndex(spaceX, spaceY);
                (tile, attributes) = GetTileData(useHighTileMapArea, useLowTileDataArea, tileMapIndex);
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
        var (tile, attributes) = GetTileData(useHighTileMapArea, useLowTileDataArea, tileMapIndex);

        if (attributes.IsVerticallyFlipped)
        {
            tileY = 7 - tileY;
        }
        for (var i = state.WindowX - 7; i < ScreenWidth; i++)
        {
            if (i < 0)
            {
                tileX++;
                continue;
            }

            var x = tileX;
            if (attributes.IsHorizontallyFlipped)
            {
                x = 7 - x;
            }
            backgroundAndWindowColors[i] = (tile.GetColorIndex(tileY, x), attributes.PalletNr);

            if (++tileX is TileSize)
            {
                tileX = 0;
                tileMapIndex++;
                (tile, attributes) = GetTileData(useHighTileMapArea, useLowTileDataArea, tileMapIndex);
            }
        }

        state.LcdLinesOfWindowDrawnThisFrame++;
    }

    private (ImprovedTile, ImprovedBackgroundAttributes) GetTileData(bool useHighTileMapArea, bool useLowTileDataArea, int tileMapIndex)
    {
        var tileDataIndex = TileMap.GetTileDataIndex(state.VideoRam, useHighTileMapArea, tileMapIndex);
        var attributes = TileMap.GetTileAttributes(state.VideoRam, useHighTileMapArea, tileMapIndex);

        return (ImprovedTileData.GetTileData(state.VideoRam, useLowTileDataArea, tileDataIndex, attributes.VramBankNr), attributes);
    }

    private void ProcessSpritesLine(IEnumerable<(ImprovedSprite, int)> spritesOnScanLine)
    {
        LcdControl control = state.LcdControl;

        foreach (var (sprite, spriteTileRow) in spritesOnScanLine)
        {
            if (IsSpriteVisible(sprite) is false)
            {
                continue;
            }

            var tileData = ImprovedTileData.GetSpriteTileData(state.VideoRam, control.IsDoubleSpriteSize
                ? (byte)(sprite.TileIndex & 0xfe)
                : sprite.TileIndex, state.VideoRamOffset);

            var tileRow = spriteTileRow;

            if (sprite.Yflip)
            {
                tileRow = (control.IsDoubleSpriteSize ? 16 : 7) - tileRow;
            }

            if (tileRow > 7)
            {
                tileRow -= 8;
                tileData = ImprovedTileData.GetSpriteTileData(state.VideoRam, (byte)(sprite.TileIndex | 1), state.VideoRamOffset);
            }

            var palette = new ImprovedColorPalette(state.ObjectColorPaletteData);

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

                if (sprite.Hidden && backgroundAndWindowColors[rowPixelIndex].Item1 > 0)
                {
                    continue;
                }

                pixelLine[rowPixelIndex] = new(palette.DecodeColorIndex(sprite.ColorPalette, colorIndex));
            }
        }

        bool IsSpriteVisible(ImprovedSprite sprite) => sprite.X is >= 0 and < ScreenWidth + 8;

        bool IsOffScreen(int screenIndex) => screenIndex is < 0 or >= ScreenWidth;
    }
}

