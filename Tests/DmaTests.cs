using Gameboi;
using Gameboi.Cartridges;
using Gameboi.Hardware;

namespace Tests;

[TestClass]
public class DmaTests
{
    private readonly SystemState state = new();
    private readonly Dma dma;
    private readonly ImprovedBus bus;


    public DmaTests()
    {
        bus = new ImprovedBus(state, new NoMemoryBankController(state));
        dma = new(state, bus);
    }

    [TestMethod]
    public void NotInProgress()
    {
        state.IsDmaInProgress = false;
        state.DmaTicksElapsed = 0;

        dma.Tick();
        dma.Tick();
        dma.Tick();

        Assert.AreEqual(false, state.IsDmaInProgress);
        Assert.AreEqual(0, state.DmaTicksElapsed);
    }

    [TestMethod]
    public void InProgress()
    {
        state.IsDmaInProgress = true;
        state.DmaTicksElapsed = 0;

        dma.Tick();
        dma.Tick();
        dma.Tick();
        dma.Tick();

        Assert.AreEqual(true, state.IsDmaInProgress);
        Assert.AreEqual(4, state.DmaTicksElapsed);
    }

    [TestMethod]
    public void Completed()
    {
        state.IsDmaInProgress = true;
        state.DmaTicksElapsed = 0;

        state.VideoRam[0] = 1;
        state.VideoRam[0x9f] = 2;

        for (var i = 0; i < (0xa0 * 4) - 1; i++)
        {
            dma.Tick();
        }
        Assert.AreEqual(true, state.IsDmaInProgress);

        dma.Tick();
        Assert.AreEqual(false, state.IsDmaInProgress);
        Assert.AreEqual(0, state.DmaTicksElapsed);

        Assert.AreEqual(1, state.Oam[0]);
        Assert.AreEqual(0, state.Oam[1]);
        Assert.AreEqual(0, state.Oam[0x9e]);
        Assert.AreEqual(2, state.Oam[0x9f]);
    }

    [TestMethod]
    public void EnabledThroughWrite()
    {
        state.IsDmaInProgress = false;
        state.DmaTicksElapsed = 0;

        state.VideoRam[0] = 1;
        state.VideoRam[0x9f] = 2;

        bus.Write(0xff46, 0x80);

        Assert.AreEqual(true, state.IsDmaInProgress);
        Assert.AreEqual(0, state.DmaTicksElapsed);

        for (var i = 0; i < (0xa0 * 4) - 1; i++)
        {
            dma.Tick();
        }
        Assert.AreEqual(true, state.IsDmaInProgress);

        dma.Tick();
        Assert.AreEqual(false, state.IsDmaInProgress);
        Assert.AreEqual(0, state.DmaTicksElapsed);

        Assert.AreEqual(1, state.Oam[0]);
        Assert.AreEqual(0, state.Oam[1]);
        Assert.AreEqual(0, state.Oam[0x9e]);
        Assert.AreEqual(2, state.Oam[0x9f]);
    }
}
