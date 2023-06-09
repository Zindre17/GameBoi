using Gameboi.Graphics;

namespace Tests;

[TestClass]
public class OamTests
{
    private readonly byte[] oam = new byte[0xa0];

    [TestMethod]
    public void GetSprites()
    {
        byte number = 0;
        foreach (var sprite in Oam.GetSprites(oam))
        {
            oam[number * 4] = number;
            oam[(number * 4) + 1] = (byte)(number * 2);
            Assert.AreEqual(number, sprite.SpriteNr);
            Assert.AreEqual(number, sprite.Y);
            Assert.AreEqual(number * 2, sprite.X);
            number++;
        }
    }
}
