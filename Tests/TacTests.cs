using Gameboi.Io;

namespace Tests;

[TestClass]
public class TacTestss
{
    [TestMethod]
    public void IsTimerEnabled()
    {
        var tac = new Tac(0);
        Assert.AreEqual(false, tac.IsTimerEnabled);

        tac = new Tac(4);
        Assert.AreEqual(true, tac.IsTimerEnabled);
        tac = new Tac(5);
        Assert.AreEqual(true, tac.IsTimerEnabled);
        tac = new Tac(6);
        Assert.AreEqual(true, tac.IsTimerEnabled);

        tac = new Tac(8);
        Assert.AreEqual(false, tac.IsTimerEnabled);
    }

    [TestMethod]
    public void TimerSpeedSelect()
    {
        var tac = new Tac(0);
        Assert.AreEqual(0, tac.TimerSpeedSelect);
        tac = new Tac(1);
        Assert.AreEqual(1, tac.TimerSpeedSelect);
        tac = new Tac(2);
        Assert.AreEqual(2, tac.TimerSpeedSelect);
        tac = new Tac(3);
        Assert.AreEqual(3, tac.TimerSpeedSelect);

        tac = new Tac(4);
        Assert.AreEqual(0, tac.TimerSpeedSelect);
        tac = new Tac(5);
        Assert.AreEqual(1, tac.TimerSpeedSelect);
        tac = new Tac(6);
        Assert.AreEqual(2, tac.TimerSpeedSelect);
        tac = new Tac(7);
        Assert.AreEqual(3, tac.TimerSpeedSelect);

        tac = new Tac(8);
        Assert.AreEqual(0, tac.TimerSpeedSelect);
    }
}
