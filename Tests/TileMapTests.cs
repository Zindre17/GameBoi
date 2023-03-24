using Gameboi.Graphics;

namespace Tests;

[TestClass]
public class TileMapTests
{
    [TestMethod]
    [DataRow(0, 0, 0)]
    [DataRow(8, 0, 1)]
    [DataRow(0, 8, 32)]
    [DataRow(8, 8, 33)]
    [DataRow(0xff, 0xff, 0x3ff)]
    public void GetTileMapIndex(int x, int y, int expected)
    {
        Assert.AreEqual(expected, TileMap.GetTileMapIndex((byte)x, (byte)y));
    }

    private readonly byte[] vram = new byte[0x2000];

    [TestMethod]
    public void GetTileDataIndex()
    {
        vram[0x1800] = 7;
        vram[0x1bff] = 9;
        vram[0x1c00] = 14;
        vram[0x1fff] = 18;

        var index = TileMap.GetTileDataIndex(vram, false, 0);
        Assert.AreEqual(7, index);

        index = TileMap.GetTileDataIndex(vram, false, 0x3ff);
        Assert.AreEqual(9, index);

        index = TileMap.GetTileDataIndex(vram, true, 0);
        Assert.AreEqual(14, index);

        index = TileMap.GetTileDataIndex(vram, true, 0x3ff);
        Assert.AreEqual(18, index);
    }
}
