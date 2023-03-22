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

    public static byte SwapNibbles(this byte value)
    {
        var low = value & 0xf;
        var high = value & 0xf0;
        return (byte)((high >> 4) | (low << 4));
    }

    public static byte Invert(this byte value) => (byte)~value;

    public static ushort Concat(this byte highByte, byte lowByte) => (ushort)((highByte << 8) | lowByte);
}
