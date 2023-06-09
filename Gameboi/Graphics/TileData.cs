namespace Gameboi.Graphics;

public static class TileData
{
    private const byte BytesPerTile = 16;

    public static Tile GetSpriteTileData(byte[] vram, byte tileIndex, int offset = 0)
    {
        return new(vram, offset + (tileIndex * BytesPerTile));
    }

    private const int HighTileDataAreaStart = 0x1000;

    public static Tile GetTileData(byte[] vram, bool useLowTileDataArea, byte tileIndex, int vramBank = 0)
    {
        var bankOffset = vramBank * 0x2_000;
        return new(vram, useLowTileDataArea
            ? (bankOffset + (tileIndex * BytesPerTile))
            : (bankOffset + HighTileDataAreaStart + ((sbyte)tileIndex * BytesPerTile)));
    }
}
