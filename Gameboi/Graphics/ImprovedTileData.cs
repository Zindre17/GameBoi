namespace Gameboi.Graphics;

public static class ImprovedTileData
{
    private const byte BytesPerTile = 16;

    public static ImprovedTile GetSpriteTileData(byte[] vram, byte tileIndex)
    {
        return new(vram, tileIndex * BytesPerTile);
    }

    private const int HighTileDataAreaStart = 0x1000;

    public static ImprovedTile GetTileData(byte[] vram, bool useLowTileDataArea, byte tileIndex, int vramBank = 0)
    {
        var bankOffset = vramBank * 0x2_000;
        return new(vram, useLowTileDataArea
            ? (bankOffset + (tileIndex * BytesPerTile))
            : (bankOffset + HighTileDataAreaStart + ((sbyte)tileIndex * BytesPerTile)));
    }
}
