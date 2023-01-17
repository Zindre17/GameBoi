using GB_Emulator.Memory;

namespace GB_Emulator.Graphics;

public interface IPallete
{
    Rgb GetColor(byte colorCode);
}

public class Palette : Register, IPallete
{
    public Palette(Byte initialValue) : base(initialValue) { }

    private static readonly Rgb black = new(0, 0, 0);
    private static readonly Rgb darkGray = new(85, 85, 85);
    private static readonly Rgb lightGray = new(170, 170, 170);
    private static readonly Rgb white = new(0xff, 0xff, 0xff);

    private static readonly Rgb[] blackWhiteColors = new[] { black, darkGray, lightGray, white };

    public Rgb GetColor(byte colorCode) => blackWhiteColors[DecodeColorNumber(colorCode)];

    private byte DecodeColorNumber(byte colorCode) => (byte)((~data >> (colorCode * 2)) & 3);
}

public class ColorPalette : IMemoryRange, IPallete
{
    private Byte palletIndex = new();
    private bool IsAutoIncrementEnabled => palletIndex[7];
    private Byte Index => palletIndex & 0x3F;
    public Address Size => 2;

    private readonly IColorAdjustment adjustment = new GameboyColorCorrection();

    private readonly Byte[] dataMemory = new Byte[64];

    public ColorPalette()
    {
        for (int i = 0; i < dataMemory.Length; i++)
        {
            dataMemory[i] = 0xFF;
        }
    }

    public Rgb GetColor(byte colorCode)
    {
        var pallet = colorCode / 4; // 4 colors per pallet
        var start = (pallet * 8) + (2 * (colorCode % 4)); // 8 byte per pallet + 2 byte per color
        var lb = dataMemory[start];
        var hb = dataMemory[start + 1];
        var color = (hb << 8) | lb;
        return adjustment.GetAdjustedColors(
            (byte)(color & 0x1f),
            (byte)((color >> 5) & 0x1f),
            (byte)((color >> 10) & 0x1f)
        );
    }

    public Byte Read(Address address, bool isCpu = false)
    {
        if (address > 1) throw new System.ArgumentOutOfRangeException(nameof(address));
        if (address == 0)
        {
            return palletIndex | 0x40;
        }
        return dataMemory[Index];
    }

    public void Write(Address address, Byte value, bool isCpu = false)
    {
        if (address > 1) throw new System.ArgumentOutOfRangeException(nameof(address));
        if (address == 0)
        {
            palletIndex = value;
            return;
        }
        dataMemory[Index] = value;
        if (IsAutoIncrementEnabled) palletIndex++;
    }

    public void Set(Address address, IMemory replacement)
    {
        throw new System.NotImplementedException();
    }
}

public interface IColorAdjustment
{
    Rgb GetAdjustedColors(byte r, byte g, byte b);
}

public class GameboyColorCorrection : IColorAdjustment
{
    const int maxColor = 960;

    private static byte ConstrictColor(int color)
    {
        return (byte)(System.Math.Min(maxColor, color) >> 2);
    }

    public Rgb GetAdjustedColors(byte r, byte g, byte b)
    {
        var newR = (r * 26) + (g * 4) + (b * 2);
        var newG = (g * 24) + (b * 8);
        var newB = (r * 6) + (g * 4) + (b * 22);


        return new(ConstrictColor(newR), ConstrictColor(newG), ConstrictColor(newB));
    }
}

