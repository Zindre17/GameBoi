
class Palette : MaskedRegister
{
    public Palette(Byte initialValue, byte mask = 0) : base(mask) { data = initialValue; }

    public Byte DecodeColorNumber(byte colorCode) => (~data >> (colorCode * 2)) & 3;
}