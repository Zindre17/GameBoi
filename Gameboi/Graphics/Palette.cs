using System;

class Palette : MaskedRegister
{
    public Palette(Byte initialValue, byte mask = 0) : base(mask) { data = initialValue; }

    public Byte DecodeColorNumber(byte colorCode)
    {
        colorCode ^= 3;
        colorCode &= 3;
        return colorCode switch
        {
            3 => data >> 6,
            2 => (data >> 4) & 3,
            1 => (data >> 2) & 3,
            0 => data & 3,
            _ => throw new Exception("Not possible"),
        };
    }
}