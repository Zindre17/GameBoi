using System;
using static ByteOperations;

class Tile
{
    private byte[] data = new byte[16];

    public Tile(byte[] data)
    {
        if (data.Length != 16)
            throw new ArgumentOutOfRangeException("Data must be 16 bytes long");

        data.CopyTo(this.data, 0);
    }

    public byte GetPaletteColor(byte x, byte y)
    {
        if (x > 7 || y > 7)
            throw new ArgumentOutOfRangeException("x and y must be lower than 8");
        byte high = GetHighBit(x, y);
        byte low = GetLowBit(x, y);
        return (byte)(high | low);
    }

    private byte GetHighBit(byte x, byte y)
    {
        byte row = data[(y * 2) + 1];
        return (byte)(GetBitAt(x, row) << 1);
    }

    private byte GetLowBit(byte x, byte y)
    {
        byte row = data[y * 2];
        return GetBitAt(x, row);
    }

    private byte GetBitAt(byte x, byte row)
    {
        return (byte)(TestBit(7 - x, row) ? 1 : 0);
    }
}