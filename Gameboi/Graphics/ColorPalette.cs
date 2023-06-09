using System;

namespace Gameboi.Graphics;

public readonly struct ColorPalette
{
    private readonly byte[] paletteData;
    private const byte BitsPerColorChannel = 5;
    private const byte BytesPerColor = 2;
    private const byte ColorsPerPalette = 4;
    private const int BytesPerPalette = ColorsPerPalette * BytesPerColor;

    public ColorPalette(byte[] data) => paletteData = data;

    public Rgb DecodeColorIndex(int palette, int colorIndex)
    {
        var colorDataIndex = (palette * BytesPerPalette) + (colorIndex * BytesPerColor);
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
