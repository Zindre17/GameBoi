using Gameboi.Graphics;

namespace Tests;

[TestClass]
public class LcdControlTests
{
    [TestMethod]
    public void IsLcdEnabled()
    {
        TestFlag(control => control.IsLcdEnabled, 7);
    }

    [TestMethod]
    public void WindowUsesHighTileMapArea()
    {
        TestFlag(control => control.WindowUsesHighTileMapArea, 6);
    }

    [TestMethod]
    public void IsWindowEnabled()
    {
        TestFlag(control => control.IsWindowEnabled, 5);
    }

    [TestMethod]
    public void BackgroundAndWindowUsesLowTileDataArea()
    {
        TestFlag(control => control.BackgroundAndWindowUsesLowTileDataArea, 4);
    }

    [TestMethod]
    public void BackgroundUsesHighTileMapArea()
    {
        TestFlag(control => control.BackgroundUsesHighTileMapArea, 3);
    }

    [TestMethod]
    public void IsDoubleSpriteSize()
    {
        TestFlag(control => control.IsDoubleSpriteSize, 2);
    }

    [TestMethod]
    public void IsSpritesEnabled()
    {
        TestFlag(control => control.IsSpritesEnabled, 1);
    }

    [TestMethod]
    public void IsBackgroundEnabled()
    {
        TestFlag(control => control.IsBackgroundEnabled, 0);
    }

    private static void TestFlag(Func<LcdControl, bool> selector, int expectedBitPosition)
    {
        var status = new LcdControl(1 << 0);
        Assert.AreEqual(expectedBitPosition is 0, selector(status));

        status = new LcdControl(1 << 1);
        Assert.AreEqual(expectedBitPosition is 1, selector(status));

        status = new LcdControl(1 << 2);
        Assert.AreEqual(expectedBitPosition is 2, selector(status));

        status = new LcdControl(1 << 3);
        Assert.AreEqual(expectedBitPosition is 3, selector(status));

        status = new LcdControl(1 << 4);
        Assert.AreEqual(expectedBitPosition is 4, selector(status));

        status = new LcdControl(1 << 5);
        Assert.AreEqual(expectedBitPosition is 5, selector(status));

        status = new LcdControl(1 << 6);
        Assert.AreEqual(expectedBitPosition is 6, selector(status));

        status = new LcdControl(1 << 7);
        Assert.AreEqual(expectedBitPosition is 7, selector(status));
    }
}
