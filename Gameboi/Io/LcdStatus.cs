using Gameboi.Extensions;

namespace Gameboi.Io;

public readonly struct LcdStatus
{
    private readonly byte value;

    public LcdStatus(byte value) => this.value = value;

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

    public static implicit operator byte(LcdStatus stat) => stat.value;
    public static implicit operator LcdStatus(byte value) => new(value);
}
