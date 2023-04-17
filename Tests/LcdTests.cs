using Gameboi;
using Gameboi.Cartridges;
using Gameboi.Graphics;
using Gameboi.Hardware;
using Gameboi.Memory.Io;
using static Gameboi.Hardware.LcdConstants;

namespace Tests;

[TestClass]
public class LcdTests
{
    [TestMethod]
    public void LcdUpdateDoesNothingWhenInactive()
    {
        var lcd = new LCD();
        lcd.Controller.IsEnabled = false;
        var startMode = lcd.Mode;

        lcd.Update(220, 1);
        var endMode = lcd.Mode;

        Assert.AreEqual(0, startMode);
        Assert.AreEqual(0, endMode);
    }

    [TestMethod]
    public void HorizontalBlankDuration()
    {
        TestModeDuration(0, 204);
    }

    [TestMethod]
    public void Mode2Duration()
    {
        TestModeDuration(2, 80);
    }

    [TestMethod]
    public void Mode3Duration()
    {
        TestModeDuration(3, 172);
    }

    [TestMethod]
    public void VerticalBlankDuration()
    {
        TestModeDuration(1, 4560);
    }


    private static void TestModeDuration(byte mode, uint cycles)
    {
        var lcd = new LCD();
        lcd.Connect(new());
        lcd.Mode = mode;
        Assert.AreEqual(mode, lcd.Mode);

        lcd.Update(cycles - 1, 1);
        Assert.AreEqual(mode, lcd.Mode);

        lcd.Update(1, 1);
        Assert.AreNotEqual(mode, lcd.Mode);
    }
}

[TestClass]
public class ImprovedLcdTests
{
    private readonly SystemState state;
    private readonly ImprovedLcd lcd;

    public ImprovedLcdTests()
    {
        state = new();
        var fakeGame = new byte[0x150];
        state.ChangeGame(fakeGame, fakeGame);
        var bus = new ImprovedBus(state, new NoMemoryBankController(state));
        var vramdma = new OldVramDmaWithNewState(state, bus);
        lcd = new(state, vramdma);
        state.Reset();
    }

    [TestMethod]
    public void ModeDurations()
    {
        state.LineY = 0;
        state.LcdStatus = new LcdStatus(0).WithMode(SearchingOam);
        state.LcdRemainingTicksInMode = SearchingOamDurationInTicks;

        for (var i = 0; i < VerticalBlankLineYStart; i++)
        {
            // Mode 2 OAM search
            if (i is not 0)
            {
                lcd.Tick();
            }
            Assert.AreEqual(SearchingOam, new LcdStatus(state.LcdStatus).Mode);

            for (var ticks = 0; ticks < SearchingOamDurationInTicks - 1; ticks++)
            {
                lcd.Tick();
                Assert.AreEqual(SearchingOam, new LcdStatus(state.LcdStatus).Mode, message: $"{i}: {ticks}");
            }

            // Mode 3 Transfer data to lcd (for line)
            lcd.Tick();
            Assert.AreEqual(TransferringDataToLcd, new LcdStatus(state.LcdStatus).Mode, message: $"{i}");

            for (var ticks = 0; ticks < GeneratePixelLineDurationInTicks - 1; ticks++)
            {
                lcd.Tick();
                Assert.AreEqual(TransferringDataToLcd, new LcdStatus(state.LcdStatus).Mode, message: $"{i}: {ticks}");
            }

            // Mode 0 H-blank
            lcd.Tick();
            Assert.AreEqual(HorizontalBlank, new LcdStatus(state.LcdStatus).Mode, message: $"{i}");

            for (var ticks = 0; ticks < HorizontalBlankDurationInTicks - 1; ticks++)
            {
                lcd.Tick();
                Assert.AreEqual(HorizontalBlank, new LcdStatus(state.LcdStatus).Mode, message: $"{i}: {ticks}");
            }
        }

        lcd.Tick();
        Assert.AreEqual(VerticalBlank, new LcdStatus(state.LcdStatus).Mode);

        // Mode 1 V-blank
        for (var i = 0; i < VerticalBlankDurationInTicks - 1; i++)
        {
            lcd.Tick();
            Assert.AreEqual(VerticalBlank, new LcdStatus(state.LcdStatus).Mode, message: $"{i}");
            Assert.AreEqual(((i + 1) / ScanLineDurationInTicks) + VerticalBlankLineYStart, state.LineY, message: $"{i}");
        }

        lcd.Tick();
        Assert.AreEqual(SearchingOam, new LcdStatus(state.LcdStatus).Mode);
    }
}
