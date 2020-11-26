
public class Palette : Register
{
    public Palette(Byte initialValue) : base(initialValue) { }

    public Byte DecodeColorNumber(byte colorCode) => (~data >> (colorCode * 2)) & 3;
}
