using Gameboi.Hardware;

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
