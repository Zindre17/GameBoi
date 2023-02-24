namespace Gameboi.Memory.Specials;

public enum CpuFlags : byte
{
    Zero = 1 << 7,
    Subtract = 1 << 6,
    HalfCarry = 1 << 5,
    Carry = 1 << 4,
}

public readonly struct CpuFlagRegister
{
    private readonly byte value;

    public CpuFlagRegister(byte value) => this.value = value;
    private CpuFlagRegister(int value) => this.value = (byte)value;

    public bool IsSet(CpuFlags flags) => (value & (byte)flags) == (byte)flags;
    public bool IsNotSet(CpuFlags flags) => (value & (byte)flags) != (byte)flags;

    public CpuFlagRegister Set(CpuFlags flags) => new(value | (byte)flags);
    public CpuFlagRegister Unset(CpuFlags flags) => new(value & ~(byte)flags);
    public CpuFlagRegister Flip(CpuFlags flags) => new(value ^ (byte)flags);

    public CpuFlagRegister SetTo(CpuFlags flags, bool on) => on ? Set(flags) : Unset(flags);

    public static implicit operator byte(CpuFlagRegister register) => register.value;
}
