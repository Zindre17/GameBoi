using Gameboi.Cartridges;
using Gameboi.Hardware;

namespace Tests;

[TestClass]
public class VideoRamBankingTests
{
    [TestMethod]
    public void Banking()
    {
        var state = new SystemState();
        var bus = new Bus(state, new NoMemoryBankController(state));

        bus.Write(0x8000, 1);
        bus.Write(0xff4f, 1);
        bus.Write(0x8000, 2);
        bus.Write(0xff4f, 0);

        Assert.AreEqual(1, state.VideoRam[0]);
        Assert.AreEqual(1, bus.Read(0x8000));
        bus.Write(0xff4f, 1);
        Assert.AreEqual(0x2000, state.VideoRamOffset);
        Assert.AreEqual(2, state.VideoRam[0x2000]);
        Assert.AreEqual(2, bus.Read(0x8000));
    }
}
