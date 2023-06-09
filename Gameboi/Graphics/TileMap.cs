namespace Gameboi.Graphics;

public static class TileMap
{
    private const int TileSize = 8;
    private const int TileMapSize = 32;

    private const int HighTileMapAreaStart = 0x1c00;
    private const int LowTileMapAreaStart = 0x1800;

    public static int GetTileMapIndex(byte x, byte y)
    {
        var firstTileMapIndexOfRow = y / TileSize * TileMapSize;
        var relativeTileMapIndexInRow = x / TileSize;

        return firstTileMapIndexOfRow + relativeTileMapIndexInRow;
    }

    public static byte GetTileDataIndex(byte[] vram, bool useHighTileMapArea, int tileMapIndex)
    {
        var tileMapStartAddress = useHighTileMapArea ? HighTileMapAreaStart : LowTileMapAreaStart;

        return vram[tileMapIndex + tileMapStartAddress];
    }

    public static BackgroundAttributes GetTileAttributes(byte[] vram, bool useHighTileMapArea, int tileMapIndex)
    {
        var tileMapStartAddress = useHighTileMapArea ? HighTileMapAreaStart : LowTileMapAreaStart;

        return new(vram[0x2000 + tileMapIndex + tileMapStartAddress]);
    }
}
