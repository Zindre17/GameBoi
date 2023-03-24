using Gameboi.Graphics;

namespace Tests;

[TestClass]
public class SpriteTests
{
    private readonly byte[] oam = new byte[0xa0];

    [TestMethod]
    public void SpriteNr()
    {
        var sprite1 = new ImprovedSprite(oam, 1);
        var sprite2 = new ImprovedSprite(oam, 2);

        Assert.AreEqual(1, sprite1.SpriteNr);
        Assert.AreEqual(2, sprite2.SpriteNr);
    }

    [TestMethod]
    public void Y()
    {
        oam[0] = 10;
        oam[4] = 20;
        var sprite = new ImprovedSprite(oam, 0);
        var nextSprite = new ImprovedSprite(oam, 1);

        Assert.AreEqual(10, sprite.Y);
        Assert.AreEqual(20, nextSprite.Y);
    }

    [TestMethod]
    public void X()
    {
        oam[1] = 11;
        var sprite = new ImprovedSprite(oam, 0);

        Assert.AreEqual(11, sprite.X);
    }

    [TestMethod]
    public void TileIndex()
    {
        oam[2] = 12;
        var sprite = new ImprovedSprite(oam, 0);

        Assert.AreEqual(12, sprite.TileIndex);
    }

    [TestMethod]
    public void Hidden()
    {
        TestFlag(sprite => sprite.Hidden, 7);
    }

    [TestMethod]
    public void Yflip()
    {
        TestFlag(sprite => sprite.Yflip, 6);
    }

    [TestMethod]
    public void Xflip()
    {
        TestFlag(sprite => sprite.Xflip, 5);
    }

    [TestMethod]
    public void UsePalette1()
    {
        TestFlag(sprite => sprite.UsePalette1, 4);
    }

    [TestMethod]
    public void VramBank()
    {
        TestFlag(sprite => sprite.VramBank is 1, 3);
    }

    [TestMethod]
    public void ColorPalette()
    {
        oam[3] = 0xff;
        var sprite = new ImprovedSprite(oam, 0);

        Assert.AreEqual(7, sprite.ColorPalette);

        oam[3] = 0;
        Assert.AreEqual(0, sprite.ColorPalette);
    }

    private void TestFlag(Func<ImprovedSprite, bool> selector, int expectedBitPosition)
    {
        var sprite = new ImprovedSprite(oam, 0);

        oam[3] = 1 << 0;
        Assert.AreEqual(expectedBitPosition is 0, selector(sprite));

        oam[3] = 1 << 1;
        Assert.AreEqual(expectedBitPosition is 1, selector(sprite));

        oam[3] = 1 << 2;
        Assert.AreEqual(expectedBitPosition is 2, selector(sprite));

        oam[3] = 1 << 3;
        Assert.AreEqual(expectedBitPosition is 3, selector(sprite));

        oam[3] = 1 << 4;
        Assert.AreEqual(expectedBitPosition is 4, selector(sprite));

        oam[3] = 1 << 5;
        Assert.AreEqual(expectedBitPosition is 5, selector(sprite));

        oam[3] = 1 << 6;
        Assert.AreEqual(expectedBitPosition is 6, selector(sprite));

        oam[3] = 1 << 7;
        Assert.AreEqual(expectedBitPosition is 7, selector(sprite));

        oam[3] = 0xff;
        Assert.AreEqual(true, selector(sprite));

        oam[3] = 0;
        Assert.AreEqual(false, selector(sprite));
    }
}
