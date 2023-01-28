namespace Gameboi.Extensions;

public static class ByteExtensions
{
    public static bool IsBitSet(this byte value, int bit)
        => (value & (1 << bit)) is not 0;

    public static byte SetBit(this byte value, int bit)
        => (byte)(value | 1 << bit);
    public static byte UnsetBit(this byte value, int bit)
        => (byte)(value & ~(1 << bit));

    public static byte SetBit(this byte value, int bit, bool on)
        => on ? value.SetBit(bit) : value.UnsetBit(bit);
}
