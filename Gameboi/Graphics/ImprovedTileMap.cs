namespace Gameboi.Graphics;

public static class TileMap
{
    private const int TileSize = 8;
    private const int TileMapSize = 32;

    private const int HighTileMapAreaStart = 0x1c00;
    private const int LowTileMapAreaStart = 0x1800;

    public static int GetTileMapIndex(byte mapX, byte mapY)
    {
        var firstTileMapIndexOfRow = mapY / TileSize * TileMapSize;
        var relativeTileMapIndexInRow = mapX / TileSize;

        return firstTileMapIndexOfRow + relativeTileMapIndexInRow;
    }

    public static byte GetTileDataIndex(byte[] vram, bool useHighTileMapArea, int tileMapIndex)
    {
        var tileMapStartAddress = useHighTileMapArea ? HighTileMapAreaStart : LowTileMapAreaStart;

        return vram[tileMapIndex + tileMapStartAddress];
    }
}
