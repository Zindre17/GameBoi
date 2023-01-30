using Gameboi.Extensions;

namespace Gameboi.Memory.Io;

public readonly struct Stat
{
    private readonly byte value;

    public Stat(byte value) => this.value = value;

    public bool IsCoincidenceInterruptEnabled => value.IsBitSet(6);
    public bool IsOAMInterruptEnabled => value.IsBitSet(5);
    public bool IsVblankInterruptEnabled => value.IsBitSet(4);
    public bool IsHblankInterruptEnabled => value.IsBitSet(3);
    public bool CoincidenceFlag => value.IsBitSet(2);

    public byte Mode => (byte)(value & 3);

    public byte WithMode(byte mode)
    {
        var exclMode = value & 0xFC;
        var sanitizedMode = mode & 3;
        return (byte)(exclMode | sanitizedMode);
    }

    public byte WithCoincidenceFlag(bool on) => value.SetBit(2, on);

    public static implicit operator byte(Stat stat) => stat.value;
    public static implicit operator Stat(byte value) => new(value);
}
