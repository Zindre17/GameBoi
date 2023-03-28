using Gameboi.Cartridges;

namespace Tests;

[TestClass]
public class GameHeaderTests
{
    private readonly GameHeader header;
    private readonly byte[] data = new byte[0x150];

    public GameHeaderTests()
    {
        header = new GameHeader(data);
    }

    [TestMethod]
    [DataRow(0x00, false)]
    [DataRow(0x7f, false)]
    [DataRow(0x80, true)]
    [DataRow(0xc0, true)]
    [DataRow(0xff, false)]
    public void IsColorGame(int value, bool expectedResult)
    {
        data[0x143] = (byte)value;
        Assert.AreEqual(expectedResult, header.IsColorGame);
    }

    [TestMethod]
    public void GetTitle()
    {
        data[0x132] = (byte)'N';
        data[0x133] = (byte)'O';
        data[0x134] = (byte)'a';
        data[0x135] = (byte)'b';
        data[0x136] = (byte)'c';
        data[0x137] = (byte)'A';
        data[0x138] = (byte)'B';
        data[0x139] = (byte)'C';
        data[0x13a] = (byte)'1';
        data[0x13b] = (byte)'2';
        data[0x13c] = (byte)'3';
        data[0x13d] = (byte)'4';
        data[0x13e] = (byte)'5';
        data[0x13f] = (byte)' ';
        data[0x140] = (byte)'6';
        data[0x141] = (byte)'9';
        data[0x142] = (byte)'9';
        data[0x143] = (byte)'6';
        data[0x144] = (byte)'4';
        data[0x145] = (byte)'2';
        data[0x146] = (byte)'0';

        Assert.AreEqual("abcABC12345 6996", header.GetTitle());
    }
}
