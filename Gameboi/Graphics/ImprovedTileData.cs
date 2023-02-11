namespace Gameboi.Graphics;

public static class ImprovedTileData
{
    private const byte BytesPerTile = 16;

    public static ImprovedTile GetSpriteTileData(byte[] vram, byte tileIndex)
    {
        return new(vram, tileIndex * BytesPerTile);
    }

    private const int HighTileDataAreaStart = 0x1000;

    public static ImprovedTile GetTileData(byte[] vram, bool useLowTileDataArea, byte tileIndex)
    {
        return new(vram, useLowTileDataArea
            ? (tileIndex * BytesPerTile)
            : (HighTileDataAreaStart + ((sbyte)tileIndex * BytesPerTile)));
    }
}
