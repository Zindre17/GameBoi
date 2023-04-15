using System;

namespace Gameboi.Graphics;

public readonly struct ImprovedPalette
{
    private readonly byte value;

    public ImprovedPalette(byte value) => this.value = value;

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

    public static implicit operator ImprovedPalette(byte value) => new(value);
}

public readonly struct ImprovedColorPalette
{
    private readonly byte[] paletteData;
    private const byte BitsPerColorChannel = 5;
    private const byte BytesPerColor = 2;
    private const byte ColorsPerPalette = 4;

    public ImprovedColorPalette(byte[] data) => paletteData = data;

    public Rgb DecodeColorIndex(int palette, int colorIndex)
    {
        var colorDataIndex = (palette * BytesPerColor * ColorsPerPalette) + (colorIndex * BytesPerColor);
        var colorData = BitConverter.ToUInt16(paletteData, colorDataIndex);
        var red = colorData & 0x1f;
        var green = (colorData >> BitsPerColorChannel) & 0x1f;
        var blue = (colorData >> (BitsPerColorChannel * 2)) & 0x1f;
        return GetAdjustedColors((byte)red, (byte)green, (byte)blue);
    }

    private const int maxColor = 960;

    private static byte ConstrictColor(int color)
    {
        return (byte)(Math.Min(maxColor, color) >> 2);
    }

    private static Rgb GetAdjustedColors(byte r, byte g, byte b)
    {
        var newR = (r * 26) + (g * 4) + (b * 2);
        var newG = (g * 24) + (b * 8);
        var newB = (r * 6) + (g * 4) + (b * 22);

        return new(ConstrictColor(newR), ConstrictColor(newG), ConstrictColor(newB));
    }
}
