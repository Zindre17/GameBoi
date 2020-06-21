using System;
using static ByteOperations;
class Palette
{
    private byte c0, c1, c2, c3;
    public Palette(byte data)
    {
        byte colors = Invert(data);
        c3 = (byte)(colors >> 6);
        c2 = (byte)((colors >> 4) & 3);
        c1 = (byte)((colors >> 2) & 3);
        c0 = (byte)(colors & 3);
    }

    public byte DecodeColorNumber(byte colorCode)
    {
        colorCode &= 3;
        if (colorCode == 3) return c3;
        if (colorCode == 2) return c2;
        if (colorCode == 1) return c1;
        if (colorCode == 0) return c0;
        throw new Exception("Not possible");
    }
}