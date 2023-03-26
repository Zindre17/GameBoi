using Gameboi;
using Gameboi.Cartridges;

namespace Tests;

[TestClass]
public class NoMbcTests
{
    private readonly SystemState state = new();
    private readonly NoMemoryBankController mbc;
    public NoMbcTests()
    {
        mbc = new NoMemoryBankController(state);
        state.ChangeGame(new byte[0x8000], new byte[0x2000], false);
    }

    [TestMethod]
    public void Read()
    {
        state.CartridgeRom[0x0000] = 1;
        state.CartridgeRom[0x3fff] = 2;
        state.CartridgeRom[0x4000] = 3;
        state.CartridgeRom[0x7fff] = 4;

        state.CartridgeRam[0x0000] = 5;
        state.CartridgeRam[0x1fff] = 6;

        Assert.AreEqual(1, mbc.ReadRom(0x0000));
        Assert.AreEqual(2, mbc.ReadRom(0x3fff));
        Assert.AreEqual(3, mbc.ReadRom(0x4000));
        Assert.AreEqual(4, mbc.ReadRom(0x7fff));

        Assert.AreEqual(5, mbc.ReadRam(0x0000));
        Assert.AreEqual(6, mbc.ReadRam(0x1fff));
    }

    [TestMethod]
    public void Write()
    {
        mbc.WriteRom(0x0001, 7);
        mbc.WriteRom(0x3ffe, 8);
        mbc.WriteRom(0x4001, 9);
        mbc.WriteRom(0x7ffe, 10);

        mbc.WriteRam(0x0001, 11);
        mbc.WriteRam(0x1ffe, 12);

        Assert.AreEqual(0, state.CartridgeRom[0x0001]);
        Assert.AreEqual(0, state.CartridgeRom[0x3ffe]);
        Assert.AreEqual(0, state.CartridgeRom[0x4001]);
        Assert.AreEqual(0, state.CartridgeRom[0x7ffe]);

        Assert.AreEqual(11, state.CartridgeRam[0x0001]);
        Assert.AreEqual(12, state.CartridgeRam[0x1ffe]);
    }
}

[TestClass]
public class Mbc1Tests
{

    private readonly SystemState state = new();
    private readonly MemoryBankController1 mbc;

    public Mbc1Tests()
    {
        mbc = new MemoryBankController1(state);
        state.ChangeGame(new byte[0x4000 * 0x80], new byte[0x8000], false);
    }

    [TestMethod]
    public void Read()
    {
        state.CartridgeRom[0x0000] = 1;
        state.CartridgeRom[0x7fff] = 2;

        state.CartridgeRam[0x0000] = 3;
        state.CartridgeRam[0x1fff] = 4;

        Assert.AreEqual(1, mbc.ReadRom(0x0000));
        Assert.AreEqual(2, mbc.ReadRom(0x7fff));

        // Ram disabled by default
        Assert.AreEqual(0xff, mbc.ReadRam(0x0000));
        Assert.AreEqual(0xff, mbc.ReadRam(0x1fff));

        // Enable ram
        state.MbcRamDisabled = false;

        Assert.AreEqual(3, mbc.ReadRam(0x0000));
        Assert.AreEqual(4, mbc.ReadRam(0x1fff));

        // Change to bank 2 at 0x4000-0x7fff
        state.CartridgeRom[0x8000] = 9;
        state.MbcRom1Offset = 0x8000;

        Assert.AreEqual(1, mbc.ReadRom(0x0000));
        Assert.AreEqual(9, mbc.ReadRom(0x4000));

        state.MbcRamOffset = 0x2000;
        state.CartridgeRam[0x2000] = 10;

        Assert.AreEqual(10, mbc.ReadRam(0));
    }

    [TestMethod]
    public void Write()
    {
        state.MbcRamDisabled = true;

        mbc.WriteRam(0x0000, 1);
        mbc.WriteRam(0x1000, 2);

        Assert.AreEqual(0, state.CartridgeRam[0x0000]);
        Assert.AreEqual(0, state.CartridgeRam[0x1000]);

        mbc.WriteRom(0x0000, 1);
        mbc.WriteRom(0x4000, 2);

        Assert.AreEqual(0, state.CartridgeRom[0x0000]);
        Assert.AreEqual(0, state.CartridgeRom[0x4000]);

        state.MbcRamDisabled = false;
        state.MbcRamOffset = 0;

        mbc.WriteRam(0x0000, 1);
        mbc.WriteRam(0x1000, 2);

        Assert.AreEqual(1, state.CartridgeRam[0x0000]);
        Assert.AreEqual(2, state.CartridgeRam[0x1000]);

        mbc.WriteRom(0x0000, 1);
        mbc.WriteRom(0x4000, 2);

        Assert.AreEqual(0, state.CartridgeRom[0x0000]);
        Assert.AreEqual(0, state.CartridgeRom[0x4000]);
    }

    [TestMethod]
    public void Banking()
    {
        state.MbcRom1Offset = 0x4000;
        Assert.AreEqual(0x4000, state.MbcRom1Offset);

        mbc.WriteRom(0x2000, 2);
        Assert.AreEqual(0x8000, state.MbcRom1Offset);

        // Test only first 5 bits used when outside bank size
        mbc.WriteRom(0x3fff, 0x1f);
        Assert.AreEqual(0x4000 * 0x1f, state.MbcRom1Offset);
        mbc.WriteRom(0x3fff, 0x21);
        Assert.AreEqual(0x4000, state.MbcRom1Offset);

        // Test ram banks
        mbc.WriteRom(0x4000, 1);
        Assert.AreEqual(0x2000, state.MbcRamOffset);
        mbc.WriteRom(0x4000, 0);
        Assert.AreEqual(0x0000, state.MbcRamOffset);

        // Enable more than 0x1f rom banks
        mbc.WriteRom(0x6000, 1);
        Assert.AreEqual(0x4000, state.MbcRom1Offset);

        mbc.WriteRom(0x4000, 1);
        Assert.AreEqual(0x4000 * 0x21, state.MbcRom1Offset);

        // Edge case 0x00 | 0x20 | 0x40 | 0x60 -> 0x01 | 0x21 | 0x41 | 0x61
        mbc.WriteRom(0x4000, 0);
        mbc.WriteRom(0x2000, 0);
        Assert.AreEqual(0x4000, state.MbcRom1Offset);

        mbc.WriteRom(0x4000, 1);
        Assert.AreEqual(0x4000 * 0x21, state.MbcRom1Offset);

        mbc.WriteRom(0x4000, 2);
        Assert.AreEqual(0x4000 * 0x41, state.MbcRom1Offset);

        mbc.WriteRom(0x4000, 3);
        Assert.AreEqual(0x4000 * 0x61, state.MbcRom1Offset);
    }

    [TestMethod]
    public void RamToggle()
    {
        state.MbcRamDisabled = true;
        Assert.AreEqual(true, state.MbcRamDisabled);

        // 0x0a enables at 0x0000 - 0x1fff
        mbc.WriteRom(0x0000, 0x0a);
        Assert.AreEqual(false, state.MbcRamDisabled);

        state.MbcRamDisabled = true;
        mbc.WriteRom(0x1fff, 0x0a);
        Assert.AreEqual(false, state.MbcRamDisabled);

        // Outside range
        state.MbcRamDisabled = true;
        mbc.WriteRom(0x2000, 0x0a);
        Assert.AreEqual(true, state.MbcRamDisabled);

        // 0xXa enables
        state.MbcRamDisabled = true;
        mbc.WriteRom(0x0000, 0x2a);
        Assert.AreEqual(false, state.MbcRamDisabled);

        state.MbcRamDisabled = true;
        mbc.WriteRom(0x0000, 0xfa);
        Assert.AreEqual(false, state.MbcRamDisabled);

        // Not 0xa in lower 4 bits disables
        state.MbcRamDisabled = false;
        mbc.WriteRom(0x0000, 0xff);
        Assert.AreEqual(true, state.MbcRamDisabled);

        state.MbcRamDisabled = false;
        mbc.WriteRom(0x0000, 0);
        Assert.AreEqual(true, state.MbcRamDisabled);
    }
}
