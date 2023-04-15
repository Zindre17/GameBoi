using Gameboi.Graphics;

namespace Tests;

[TestClass]
public class TileDataTests
{
    private readonly byte[] vram = new byte[0x2000];

    [TestMethod]
    public void GetSpriteTileData()
    {
        vram[0] = 3;
        vram[1] = 6;
        var tile = ImprovedTileData.GetSpriteTileData(vram, 0);

        Assert.AreEqual(0b_01, tile.GetColorIndex(0, 7));
        Assert.AreEqual(0b_11, tile.GetColorIndex(0, 6));
        Assert.AreEqual(0b_10, tile.GetColorIndex(0, 5));

        vram[16] = 6;
        vram[17] = 3;
        tile = ImprovedTileData.GetSpriteTileData(vram, 1);

        Assert.AreEqual(0b_10, tile.GetColorIndex(0, 7));
        Assert.AreEqual(0b_11, tile.GetColorIndex(0, 6));
        Assert.AreEqual(0b_01, tile.GetColorIndex(0, 5));

        vram[0x1000 - 16] = 1;
        vram[0x1000 - 15] = 3;
        tile = ImprovedTileData.GetSpriteTileData(vram, 0xff);

        Assert.AreEqual(0b_11, tile.GetColorIndex(0, 7));
        Assert.AreEqual(0b_10, tile.GetColorIndex(0, 6));
        Assert.AreEqual(0b_00, tile.GetColorIndex(0, 5));
    }

    [TestMethod]
    public void GetTileData()
    {
        vram[0] = 3;
        vram[1] = 6;

        var tile = ImprovedTileData.GetTileData(vram, true, 0);
        Assert.AreEqual(0b_01, tile.GetColorIndex(0, 7));
        Assert.AreEqual(0b_11, tile.GetColorIndex(0, 6));
        Assert.AreEqual(0b_10, tile.GetColorIndex(0, 5));

        vram[0x1000] = 6;
        vram[0x1001] = 3;

        tile = ImprovedTileData.GetTileData(vram, false, 0);
        Assert.AreEqual(0b_10, tile.GetColorIndex(0, 7));
        Assert.AreEqual(0b_11, tile.GetColorIndex(0, 6));
        Assert.AreEqual(0b_01, tile.GetColorIndex(0, 5));

        vram[0x1000 - 16] = 1;
        vram[0x1000 - 15] = 3;

        tile = ImprovedTileData.GetTileData(vram, true, 0xff);
        Assert.AreEqual(0b_11, tile.GetColorIndex(0, 7));
        Assert.AreEqual(0b_10, tile.GetColorIndex(0, 6));
        Assert.AreEqual(0b_00, tile.GetColorIndex(0, 5));

        tile = ImprovedTileData.GetTileData(vram, false, 0xff);
        Assert.AreEqual(0b_11, tile.GetColorIndex(0, 7));
        Assert.AreEqual(0b_10, tile.GetColorIndex(0, 6));
        Assert.AreEqual(0b_00, tile.GetColorIndex(0, 5));

        vram[0x800] = 3;
        vram[0x801] = 1;

        tile = ImprovedTileData.GetTileData(vram, false, 0x80);
        Assert.AreEqual(0b_11, tile.GetColorIndex(0, 7));
        Assert.AreEqual(0b_01, tile.GetColorIndex(0, 6));
        Assert.AreEqual(0b_00, tile.GetColorIndex(0, 5));

        tile = ImprovedTileData.GetTileData(vram, true, 0x80);
        Assert.AreEqual(0b_11, tile.GetColorIndex(0, 7));
        Assert.AreEqual(0b_01, tile.GetColorIndex(0, 6));
        Assert.AreEqual(0b_00, tile.GetColorIndex(0, 5));

        vram[0x1800 - 16] = 2;
        vram[0x1800 - 15] = 1;

        tile = ImprovedTileData.GetTileData(vram, false, 0x7f);
        Assert.AreEqual(0b_10, tile.GetColorIndex(0, 7));
        Assert.AreEqual(0b_01, tile.GetColorIndex(0, 6));
        Assert.AreEqual(0b_00, tile.GetColorIndex(0, 5));
    }
}
