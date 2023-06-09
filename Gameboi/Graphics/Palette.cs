namespace Gameboi.Graphics;

public readonly struct Palette
{
    private readonly byte value;

    public Palette(byte value) => this.value = value;

    private const byte BitsPerColor = 2;

    public Rgb DecodeColorIndex(int colorIndex)
    {
        var colorCode = (value >> (colorIndex * BitsPerColor)) & 3;
        return colorCode switch
        {
            0 => Rgb.white,
            1 => Rgb.lightGray,
            2 => Rgb.darkGray,
            _ => Rgb.black,
        };
    }

    public static implicit operator Palette(byte value) => new(value);
}
