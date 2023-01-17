namespace Gameboi.Graphics;

public record struct Rgba(byte Red, byte Green, byte Blue, byte Alpha)
{
    public Rgba(Rgb color, byte alpha = 0xff)
        : this(color.Red, color.Green, color.Blue, alpha)
    { }
}
