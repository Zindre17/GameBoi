using Gameboi.Memory.Specials;

namespace Tests;

[TestClass]
public class CpuFlagTests
{
    [TestMethod]
    public void IsSet()
    {
        var flags = new CpuFlagRegister(0xff);

        Assert.AreEqual(true, flags.IsSet(CpuFlags.Carry));
        Assert.AreEqual(true, flags.IsSet(CpuFlags.HalfCarry));
        Assert.AreEqual(true, flags.IsSet(CpuFlags.Subtract));
        Assert.AreEqual(true, flags.IsSet(CpuFlags.Zero));

        Assert.AreEqual(true, flags.IsSet(CpuFlags.Zero | CpuFlags.Subtract | CpuFlags.HalfCarry | CpuFlags.Carry));

        flags = new CpuFlagRegister(0);

        Assert.AreEqual(false, flags.IsSet(CpuFlags.Carry));
        Assert.AreEqual(false, flags.IsSet(CpuFlags.HalfCarry));
        Assert.AreEqual(false, flags.IsSet(CpuFlags.Subtract));
        Assert.AreEqual(false, flags.IsSet(CpuFlags.Zero));

        Assert.AreEqual(false, flags.IsSet(CpuFlags.Zero | CpuFlags.Subtract | CpuFlags.HalfCarry | CpuFlags.Carry));
    }

    [TestMethod]
    public void IsNotSet()
    {
        var flags = new CpuFlagRegister(0xff);

        Assert.AreEqual(false, flags.IsNotSet(CpuFlags.Carry));
        Assert.AreEqual(false, flags.IsNotSet(CpuFlags.HalfCarry));
        Assert.AreEqual(false, flags.IsNotSet(CpuFlags.Subtract));
        Assert.AreEqual(false, flags.IsNotSet(CpuFlags.Zero));

        Assert.AreEqual(false, flags.IsNotSet(CpuFlags.Zero | CpuFlags.Subtract | CpuFlags.HalfCarry | CpuFlags.Carry));

        flags = new CpuFlagRegister(0);

        Assert.AreEqual(true, flags.IsNotSet(CpuFlags.Carry));
        Assert.AreEqual(true, flags.IsNotSet(CpuFlags.HalfCarry));
        Assert.AreEqual(true, flags.IsNotSet(CpuFlags.Subtract));
        Assert.AreEqual(true, flags.IsNotSet(CpuFlags.Zero));

        Assert.AreEqual(true, flags.IsNotSet(CpuFlags.Zero | CpuFlags.Subtract | CpuFlags.HalfCarry | CpuFlags.Carry));
    }

    [TestMethod]
    public void Set()
    {
        var flags = new CpuFlagRegister(0);
        flags = flags.Set(CpuFlags.Zero);

        Assert.AreEqual(false, flags.IsSet(CpuFlags.Carry));
        Assert.AreEqual(false, flags.IsSet(CpuFlags.HalfCarry));
        Assert.AreEqual(false, flags.IsSet(CpuFlags.Subtract));
        Assert.AreEqual(true, flags.IsSet(CpuFlags.Zero));

        Assert.AreEqual(false, flags.IsSet(CpuFlags.Zero | CpuFlags.Subtract | CpuFlags.HalfCarry | CpuFlags.Carry));

        flags = flags.Set(CpuFlags.Subtract | CpuFlags.Carry);

        Assert.AreEqual(true, flags.IsSet(CpuFlags.Carry));
        Assert.AreEqual(false, flags.IsSet(CpuFlags.HalfCarry));
        Assert.AreEqual(true, flags.IsSet(CpuFlags.Subtract));
        Assert.AreEqual(true, flags.IsSet(CpuFlags.Zero));

        Assert.AreEqual(false, flags.IsSet(CpuFlags.Zero | CpuFlags.Subtract | CpuFlags.HalfCarry | CpuFlags.Carry));
    }

    [TestMethod]
    public void Unset()
    {
        var flags = new CpuFlagRegister(0xff);
        flags = flags.Unset(CpuFlags.Zero);

        Assert.AreEqual(false, flags.IsNotSet(CpuFlags.Carry));
        Assert.AreEqual(false, flags.IsNotSet(CpuFlags.HalfCarry));
        Assert.AreEqual(false, flags.IsNotSet(CpuFlags.Subtract));
        Assert.AreEqual(true, flags.IsNotSet(CpuFlags.Zero));

        flags = flags.Unset(CpuFlags.Subtract | CpuFlags.Carry);

        Assert.AreEqual(true, flags.IsNotSet(CpuFlags.Carry));
        Assert.AreEqual(false, flags.IsNotSet(CpuFlags.HalfCarry));
        Assert.AreEqual(true, flags.IsNotSet(CpuFlags.Subtract));
        Assert.AreEqual(true, flags.IsNotSet(CpuFlags.Zero));
    }

    [TestMethod]
    public void SetTo()
    {
        var flags = new CpuFlagRegister(0xff);
        flags = flags.SetTo(CpuFlags.Zero, false);

        Assert.AreEqual(false, flags.IsNotSet(CpuFlags.Carry));
        Assert.AreEqual(false, flags.IsNotSet(CpuFlags.HalfCarry));
        Assert.AreEqual(false, flags.IsNotSet(CpuFlags.Subtract));
        Assert.AreEqual(true, flags.IsNotSet(CpuFlags.Zero));

        flags = flags.SetTo(CpuFlags.Subtract | CpuFlags.Carry, false);

        Assert.AreEqual(true, flags.IsNotSet(CpuFlags.Carry));
        Assert.AreEqual(false, flags.IsNotSet(CpuFlags.HalfCarry));
        Assert.AreEqual(true, flags.IsNotSet(CpuFlags.Subtract));
        Assert.AreEqual(true, flags.IsNotSet(CpuFlags.Zero));

        flags = flags.SetTo(CpuFlags.Subtract, true);

        Assert.AreEqual(false, flags.IsSet(CpuFlags.Carry));
        Assert.AreEqual(true, flags.IsSet(CpuFlags.HalfCarry));
        Assert.AreEqual(true, flags.IsSet(CpuFlags.Subtract));
        Assert.AreEqual(false, flags.IsSet(CpuFlags.Zero));
    }

    [TestMethod]
    public void Flip()
    {
        var flags = new CpuFlagRegister(0xff);
        flags = flags.Flip(CpuFlags.Zero);

        Assert.AreEqual(false, flags.IsNotSet(CpuFlags.Carry));
        Assert.AreEqual(false, flags.IsNotSet(CpuFlags.HalfCarry));
        Assert.AreEqual(false, flags.IsNotSet(CpuFlags.Subtract));
        Assert.AreEqual(true, flags.IsNotSet(CpuFlags.Zero));

        flags = flags.Flip(CpuFlags.Zero | CpuFlags.Carry);

        Assert.AreEqual(false, flags.IsSet(CpuFlags.Carry));
        Assert.AreEqual(true, flags.IsSet(CpuFlags.HalfCarry));
        Assert.AreEqual(true, flags.IsSet(CpuFlags.Subtract));
        Assert.AreEqual(true, flags.IsSet(CpuFlags.Zero));
    }
}
