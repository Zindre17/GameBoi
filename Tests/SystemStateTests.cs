using Gameboi;

namespace Tests;

[TestClass]
public class SystemStateTests
{
    private readonly SystemState state = new();

    [TestMethod]
    public void ReadValueFromRefOfByte()
    {
        state.Accumulator = 7;

        Assert.AreEqual(7, state.Accumulator);
    }

    [TestMethod]
    public void WriteValueToRefOfByte()
    {
        ref var a = ref state.Accumulator;
        a = 7;

        Assert.AreEqual(7, a);
        Assert.AreEqual(7, state.Accumulator);
    }

    [TestMethod]
    public void ChangeGame()
    {
        var gameRom = new byte[] { 1 };

        // Ram should be reset on game change;
        var gameRam = new byte[] { 2 };
        state.ChangeGame(gameRom, gameRam, false);

        Assert.AreEqual(1, state.CartridgeRom[0]);
        Assert.AreEqual(0, state.CartridgeRam[0]);
        Assert.AreEqual(0, gameRam[0]);

        Assert.ThrowsException<IndexOutOfRangeException>(() => state.CartridgeRom[1]);
        Assert.ThrowsException<IndexOutOfRangeException>(() => state.CartridgeRam[1]);
    }

    [TestMethod]
    public void BC()
    {
        state.B = 1;
        state.C = 2;

        Assert.AreEqual(258, state.BC);
    }

    [TestMethod]
    public void DE()
    {
        state.D = 1;
        state.E = 2;

        Assert.AreEqual(258, state.DE);
    }

    [TestMethod]
    public void HL()
    {
        state.High = 1;
        state.Low = 2;

        Assert.AreEqual(258, state.HL);
    }

    [TestMethod]
    public void IoShortcuts()
    {
        // TODO: make sure all shortcuts use correct addresses
        state.IoPorts[IoIndices.IF_index] = 10;

        Assert.AreEqual(10, state.InterruptFlags);
    }

    // TODO: test reset functionality
}
