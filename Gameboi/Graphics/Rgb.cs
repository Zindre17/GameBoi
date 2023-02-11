namespace Gameboi.Graphics;

public record struct Rgb(byte Red, byte Green, byte Blue)
{
    public static readonly Rgb black = new(0, 0, 0);
    public static readonly Rgb darkGray = new(85, 85, 85);
    public static readonly Rgb lightGray = new(170, 170, 170);
    public static readonly Rgb white = new(0xff, 0xff, 0xff);
}

