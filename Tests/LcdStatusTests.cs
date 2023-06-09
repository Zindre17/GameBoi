using Gameboi.Io;

namespace Tests;

[TestClass]
public class LcdStatusTests
{

    [TestMethod]
    public void Mode()
    {
        var status = new LcdStatus(0);
        Assert.AreEqual(0, status.Mode);

        status = new LcdStatus(1);
        Assert.AreEqual(1, status.Mode);

        status = new LcdStatus(2);
        Assert.AreEqual(2, status.Mode);

        status = new LcdStatus(3);
        Assert.AreEqual(3, status.Mode);

        status = new LcdStatus(4);
        Assert.AreEqual(0, status.Mode);

        status = new LcdStatus(0xff);
        Assert.AreEqual(3, status.Mode);
    }

    [TestMethod]
    public void CoincidenceFlag()
    {
        var status = new LcdStatus(1 << 0);
        Assert.AreEqual(false, status.CoincidenceFlag);
        status = new LcdStatus(1 << 1);
        Assert.AreEqual(false, status.CoincidenceFlag);

        status = new LcdStatus(1 << 2);
        Assert.AreEqual(true, status.CoincidenceFlag);

        status = new LcdStatus(1 << 3);
        Assert.AreEqual(false, status.CoincidenceFlag);
        status = new LcdStatus(1 << 4);
        Assert.AreEqual(false, status.CoincidenceFlag);
        status = new LcdStatus(1 << 5);
        Assert.AreEqual(false, status.CoincidenceFlag);
        status = new LcdStatus(1 << 6);
        Assert.AreEqual(false, status.CoincidenceFlag);
        status = new LcdStatus(1 << 7);
        Assert.AreEqual(false, status.CoincidenceFlag);
    }

    [TestMethod]
    public void CoincidenceInterruptEnabled()
    {
        var status = new LcdStatus(1 << 0);
        Assert.AreEqual(false, status.IsCoincidenceInterruptEnabled);
        status = new LcdStatus(1 << 1);
        Assert.AreEqual(false, status.IsCoincidenceInterruptEnabled);
        status = new LcdStatus(1 << 2);
        Assert.AreEqual(false, status.IsCoincidenceInterruptEnabled);
        status = new LcdStatus(1 << 3);
        Assert.AreEqual(false, status.IsCoincidenceInterruptEnabled);
        status = new LcdStatus(1 << 4);
        Assert.AreEqual(false, status.IsCoincidenceInterruptEnabled);
        status = new LcdStatus(1 << 5);
        Assert.AreEqual(false, status.IsCoincidenceInterruptEnabled);

        status = new LcdStatus(1 << 6);
        Assert.AreEqual(true, status.IsCoincidenceInterruptEnabled);

        status = new LcdStatus(1 << 7);
        Assert.AreEqual(false, status.IsCoincidenceInterruptEnabled);
    }

    [TestMethod]
    public void HblankInterruptEnabled()
    {
        var status = new LcdStatus(1 << 0);
        Assert.AreEqual(false, status.IsHblankInterruptEnabled);
        status = new LcdStatus(1 << 1);
        Assert.AreEqual(false, status.IsHblankInterruptEnabled);
        status = new LcdStatus(1 << 2);
        Assert.AreEqual(false, status.IsHblankInterruptEnabled);

        status = new LcdStatus(1 << 3);
        Assert.AreEqual(true, status.IsHblankInterruptEnabled);

        status = new LcdStatus(1 << 4);
        Assert.AreEqual(false, status.IsHblankInterruptEnabled);
        status = new LcdStatus(1 << 5);
        Assert.AreEqual(false, status.IsHblankInterruptEnabled);
        status = new LcdStatus(1 << 6);
        Assert.AreEqual(false, status.IsHblankInterruptEnabled);
        status = new LcdStatus(1 << 7);
        Assert.AreEqual(false, status.IsHblankInterruptEnabled);
    }

    [TestMethod]
    public void VblankInterruptEnabled()
    {
        var status = new LcdStatus(1 << 0);
        Assert.AreEqual(false, status.IsVblankInterruptEnabled);
        status = new LcdStatus(1 << 1);
        Assert.AreEqual(false, status.IsVblankInterruptEnabled);
        status = new LcdStatus(1 << 2);
        Assert.AreEqual(false, status.IsVblankInterruptEnabled);
        status = new LcdStatus(1 << 3);
        Assert.AreEqual(false, status.IsVblankInterruptEnabled);

        status = new LcdStatus(1 << 4);
        Assert.AreEqual(true, status.IsVblankInterruptEnabled);

        status = new LcdStatus(1 << 5);
        Assert.AreEqual(false, status.IsVblankInterruptEnabled);
        status = new LcdStatus(1 << 6);
        Assert.AreEqual(false, status.IsVblankInterruptEnabled);
        status = new LcdStatus(1 << 7);
        Assert.AreEqual(false, status.IsVblankInterruptEnabled);
    }

    [TestMethod]
    public void OamInterruptEnabled()
    {
        var status = new LcdStatus(1 << 0);
        Assert.AreEqual(false, status.IsOAMInterruptEnabled);
        status = new LcdStatus(1 << 1);
        Assert.AreEqual(false, status.IsOAMInterruptEnabled);
        status = new LcdStatus(1 << 2);
        Assert.AreEqual(false, status.IsOAMInterruptEnabled);
        status = new LcdStatus(1 << 3);
        Assert.AreEqual(false, status.IsOAMInterruptEnabled);
        status = new LcdStatus(1 << 4);
        Assert.AreEqual(false, status.IsOAMInterruptEnabled);

        status = new LcdStatus(1 << 5);
        Assert.AreEqual(true, status.IsOAMInterruptEnabled);

        status = new LcdStatus(1 << 6);
        Assert.AreEqual(false, status.IsOAMInterruptEnabled);
        status = new LcdStatus(1 << 7);
        Assert.AreEqual(false, status.IsOAMInterruptEnabled);
    }

    [TestMethod]
    public void WithMode()
    {
        var status = new LcdStatus(0xff);

        var newValue = status.WithMode(0);
        Assert.AreEqual(0b1111_1100, newValue);

        newValue = status.WithMode(1);
        Assert.AreEqual(0b1111_1101, newValue);

        newValue = status.WithMode(2);
        Assert.AreEqual(0b1111_1110, newValue);

        newValue = status.WithMode(3);
        Assert.AreEqual(0xff, newValue);
    }

    [TestMethod]
    public void WithCoincidenceFlag()
    {
        var status = new LcdStatus(0xff);

        var newValue = status.WithCoincidenceFlag(true);
        Assert.AreEqual(0xff, newValue);

        newValue = status.WithCoincidenceFlag(false);
        Assert.AreEqual(0b1111_1011, newValue);
    }
}
