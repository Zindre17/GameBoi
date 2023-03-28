using Gameboi.Cartridges;

namespace Tests;

[TestClass]
public class RomReaderTests
{
    [TestMethod]
    [DataRow(0x00, MbcType.NoMbc, false)]
    [DataRow(0x01, MbcType.Mbc1, false)]
    [DataRow(0x02, MbcType.Mbc1, true)]
    [DataRow(0x03, MbcType.Mbc1, true)]
    [DataRow(0x05, MbcType.Mbc2, false)]
    [DataRow(0x06, MbcType.Mbc2, false)]
    [DataRow(0x08, MbcType.NoMbc, true)]
    [DataRow(0x09, MbcType.NoMbc, true)]
    // [DataRow(0x0b, MbcType.MMM01, false)]
    // [DataRow(0x0c, MbcType.MMM01, true)]
    // [DataRow(0x0d, MbcType.MMM01, true)]
    [DataRow(0x0f, MbcType.Mbc3, false)]
    [DataRow(0x10, MbcType.Mbc3, true)]
    [DataRow(0x11, MbcType.Mbc3, false)]
    [DataRow(0x12, MbcType.Mbc3, true)]
    [DataRow(0x13, MbcType.Mbc3, true)]
    [DataRow(0x19, MbcType.Mbc5, false)]
    [DataRow(0x1a, MbcType.Mbc5, true)]
    [DataRow(0x1b, MbcType.Mbc5, true)]
    [DataRow(0x1c, MbcType.Mbc5, false)]
    [DataRow(0x1d, MbcType.Mbc5, true)]
    [DataRow(0x1e, MbcType.Mbc5, true)]
    public void CartridgeType(int code, MbcType expectedType, bool expectedRamStatus)
    {
        var (type, ramStatus) = RomReader.GetCartridgeType((byte)code);
        Assert.AreEqual(expectedType, type);
        Assert.AreEqual(expectedRamStatus, ramStatus);
    }

    [TestMethod]
    [DataRow(0, 2)]
    [DataRow(1, 4)]
    [DataRow(2, 8)]
    [DataRow(3, 16)]
    [DataRow(4, 32)]
    [DataRow(5, 64)]
    [DataRow(6, 128)]
    [DataRow(7, 256)]
    [DataRow(8, 512)]
    [DataRow(0x52, 72)]
    [DataRow(0x53, 80)]
    [DataRow(0x54, 96)]
    public void RomSize(int value, int expectedBankCount)
    {
        Assert.AreEqual(expectedBankCount, RomReader.TranslateRomSizeTypeToBanks((byte)value));
    }
}
