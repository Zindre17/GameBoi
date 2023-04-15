using Gameboi.Graphics;

namespace Tests;

[TestClass]
public class TileTests
{
    private readonly byte[] data = new byte[32];

    [TestMethod]
    public void GetColorIndex()
    {
        // looks like this: 00 10 11 11 11 11 10 00
        data[0] = 0x3c;
        data[1] = 0x7e;

        var tile1 = new ImprovedTile(data, 0);

        Assert.AreEqual(0b00, tile1.GetColorIndex(0, 0));
        Assert.AreEqual(0b10, tile1.GetColorIndex(0, 1));
        Assert.AreEqual(0b11, tile1.GetColorIndex(0, 2));
        Assert.AreEqual(0b11, tile1.GetColorIndex(0, 3));
        Assert.AreEqual(0b11, tile1.GetColorIndex(0, 4));
        Assert.AreEqual(0b11, tile1.GetColorIndex(0, 5));
        Assert.AreEqual(0b10, tile1.GetColorIndex(0, 6));
        Assert.AreEqual(0b00, tile1.GetColorIndex(0, 7));

        //looks like this: 01 10 01 11 11 10 01 10
        data[16] = 0b1011_1010;
        data[17] = 0b0101_1101;

        var tile2 = new ImprovedTile(data, 16);

        Assert.AreEqual(0b01, tile2.GetColorIndex(0, 0));
        Assert.AreEqual(0b10, tile2.GetColorIndex(0, 1));
        Assert.AreEqual(0b01, tile2.GetColorIndex(0, 2));
        Assert.AreEqual(0b11, tile2.GetColorIndex(0, 3));
        Assert.AreEqual(0b11, tile2.GetColorIndex(0, 4));
        Assert.AreEqual(0b10, tile2.GetColorIndex(0, 5));
        Assert.AreEqual(0b01, tile2.GetColorIndex(0, 6));
        Assert.AreEqual(0b10, tile2.GetColorIndex(0, 7));
    }
}
