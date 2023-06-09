using Gameboi;
using Gameboi.Cartridges;
using Gameboi.Hardware;

namespace Tests;

[TestClass]
public class BusTests
{
    private readonly SystemState state;
    private readonly Bus bus;

    public BusTests()
    {
        state = new SystemState();
        state.ChangeGame(new byte[0x8000], new byte[0x2000]);
        bus = new(state, new NoMemoryBankController(state));
    }

    [TestMethod]
    public void ReadCartridgeRom()
    {
        state.CartridgeRom[0] = 1;
        state.CartridgeRom[0x3fff] = 2;
        state.CartridgeRom[0x4000] = 3;
        state.CartridgeRom[0x7fff] = 4;

        Assert.AreEqual(1, bus.Read(0));
        Assert.AreEqual(2, bus.Read(0x3fff));
        Assert.AreEqual(3, bus.Read(0x4000));
        Assert.AreEqual(4, bus.Read(0x7fff));
    }

    [TestMethod]
    public void ReadVideoRam()
    {
        state.VideoRam[0] = 1;
        state.VideoRam[0x0fff] = 2;
        state.VideoRam[0x1fff] = 3;

        Assert.AreEqual(1, bus.Read(0x8000));
        Assert.AreEqual(2, bus.Read(0x8fff));
        Assert.AreEqual(3, bus.Read(0x9fff));
    }

    [TestMethod]
    public void ReadCartridgeRam()
    {
        state.CartridgeRam[0] = 1;
        state.CartridgeRam[0x0fff] = 2;
        state.CartridgeRam[0x1fff] = 3;

        Assert.AreEqual(1, bus.Read(0xa000));
        Assert.AreEqual(2, bus.Read(0xafff));
        Assert.AreEqual(3, bus.Read(0xbfff));
    }

    [TestMethod]
    public void ReadWorkRam()
    {
        state.WorkRam[0] = 1;
        state.WorkRam[0x0fff] = 2;
        state.WorkRam[0x1dff] = 3;
        state.WorkRam[0x1fff] = 4;

        Assert.AreEqual(1, bus.Read(0xc000));
        Assert.AreEqual(2, bus.Read(0xcfff));
        Assert.AreEqual(3, bus.Read(0xddff));
        Assert.AreEqual(4, bus.Read(0xdfff));
        // Read from echo as well
        Assert.AreEqual(1, bus.Read(0xe000));
        Assert.AreEqual(2, bus.Read(0xefff));
        Assert.AreEqual(3, bus.Read(0xfdff));
        // Outside echo
        Assert.AreNotEqual(4, bus.Read(0xffff));
    }

    [TestMethod]
    public void ReadOam()
    {
        state.Oam[0] = 1;
        state.Oam[0x50] = 2;
        state.Oam[0x9f] = 3;

        Assert.AreEqual(1, bus.Read(0xfe00));
        Assert.AreEqual(2, bus.Read(0xfe50));
        Assert.AreEqual(3, bus.Read(0xfe9f));

        Assert.ThrowsException<IndexOutOfRangeException>(() => state.Oam[0xa0]);
    }

    [TestMethod]
    public void ReadUnused()
    {
        // Just before unused
        Assert.AreNotEqual(0xff, bus.Read(0xfe9f));

        // Start of unused
        Assert.AreEqual(0xff, bus.Read(0xfea0));
        // End of unused
        Assert.AreEqual(0xff, bus.Read(0xfeff));

        //Just after end of unused
        Assert.AreNotEqual(0xff, bus.Read(0xff00));
    }

    [TestMethod]
    public void ReadIo()
    {
        // TODO: properly check that special IO registers have correct read masks etc.
        state.IoPorts[0] = 1;
        state.IoPorts[0x7f] = 2;

        Assert.AreEqual(192 + 1, bus.Read(0xff00));
        Assert.AreEqual(2, bus.Read(0xff7f));

        Assert.ThrowsException<IndexOutOfRangeException>(() => state.IoPorts[0x80]);
    }

    [TestMethod]
    public void ReadHighRam()
    {
        state.HighRam[0] = 1;
        state.HighRam[0x7e] = 2;

        Assert.AreEqual(1, bus.Read(0xff80));
        Assert.AreEqual(2, bus.Read(0xfffe));

        Assert.ThrowsException<IndexOutOfRangeException>(() => state.HighRam[0x7f]);
    }

    [TestMethod]
    public void ReadInterruptEnable()
    {
        state.InterruptEnableRegister = 1;

        Assert.AreEqual(1, bus.Read(0xffff));
    }
    [TestMethod]
    public void WriteCartridgeRom()
    {
        bus.Write(0x0000, 1);
        bus.Write(0x3fff, 2);
        bus.Write(0x4000, 3);
        bus.Write(0x7fff, 4);

        // Read only, should not be updated
        Assert.AreNotEqual(1, state.CartridgeRom[0]);
        Assert.AreNotEqual(2, state.CartridgeRom[0x3fff]);
        Assert.AreNotEqual(3, state.CartridgeRom[0x4000]);
        Assert.AreNotEqual(4, state.CartridgeRom[0x7fff]);
    }

    [TestMethod]
    public void WriteVideoRam()
    {
        bus.Write(0x8000, 1);
        bus.Write(0x8fff, 2);
        bus.Write(0x9fff, 3);

        Assert.AreEqual(1, state.VideoRam[0]);
        Assert.AreEqual(2, state.VideoRam[0x0fff]);
        Assert.AreEqual(3, state.VideoRam[0x1fff]);
    }

    [TestMethod]
    public void WriteCartridgeRam()
    {
        bus.Write(0xa000, 1);
        bus.Write(0xafff, 2);
        bus.Write(0xbfff, 3);

        Assert.AreEqual(1, state.CartridgeRam[0]);
        Assert.AreEqual(2, state.CartridgeRam[0x0fff]);
        Assert.AreEqual(3, state.CartridgeRam[0x1fff]);
    }

    [TestMethod]
    public void WriteWorkRam()
    {
        bus.Write(0xc000, 1);
        bus.Write(0xcfff, 2);
        bus.Write(0xddff, 3);
        bus.Write(0xdfff, 4);

        // Write to echo as well
        bus.Write(0xe001, 5);
        bus.Write(0xeffe, 6);
        bus.Write(0xfdfe, 7);

        // Outside echo
        bus.Write(0xfffe, 8);

        Assert.AreEqual(1, state.WorkRam[0x0000]);
        Assert.AreEqual(2, state.WorkRam[0x0fff]);
        Assert.AreEqual(3, state.WorkRam[0x1dff]);
        Assert.AreEqual(4, state.WorkRam[0x1fff]);

        Assert.AreEqual(5, state.WorkRam[0x0001]);
        Assert.AreEqual(6, state.WorkRam[0x0ffe]);
        Assert.AreEqual(7, state.WorkRam[0x1dfe]);

        Assert.AreNotEqual(8, state.WorkRam[0x1ffe]);
    }

    [TestMethod]
    public void WriteOam()
    {
        bus.Write(0xfe00, 1);
        bus.Write(0xfe50, 2);
        bus.Write(0xfe9f, 3);

        Assert.AreEqual(1, state.Oam[0]);
        Assert.AreEqual(2, state.Oam[0x50]);
        Assert.AreEqual(3, state.Oam[0x9f]);

        Assert.ThrowsException<IndexOutOfRangeException>(() => state.Oam[0xa0]);
    }

    [TestMethod]
    public void WriteUnused()
    {
        // Nothing should happen
        bus.Write(0xfea0, 1);
        bus.Write(0xfeff, 2);

        Assert.AreEqual(0xff, bus.Read(0xfea0));
        Assert.AreEqual(0xff, bus.Read(0xfeff));
    }

    [TestMethod]
    public void WriteIo()
    {
        // TODO: properly check that special IO registers have correct write behavior"
        bus.Write(0xff00, 1);
        bus.Write(0xff7f, 2);

        Assert.AreEqual(0, state.IoPorts[0]);
        Assert.AreEqual(2, state.IoPorts[0x7f]);
    }

    [TestMethod]
    public void WriteHighRam()
    {
        bus.Write(0xff80, 1);
        bus.Write(0xfffe, 2);

        Assert.AreEqual(1, state.HighRam[0]);
        Assert.AreEqual(2, state.HighRam[0x7e]);
    }

    [TestMethod]
    public void WriteInterruptEnable()
    {
        bus.Write(0xffff, 1);

        Assert.AreEqual(1, state.InterruptEnableRegister);
    }
}
