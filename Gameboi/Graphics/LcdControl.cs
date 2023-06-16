using Gameboi.Extensions;

namespace Gameboi.Graphics;

public readonly struct LcdControl
{
    private readonly byte value;

    public LcdControl(byte value) => this.value = value;

    public bool IsLcdEnabled => value.IsBitSet(7);
    public bool WindowUsesHighTileMapArea => value.IsBitSet(6);
    public bool IsWindowEnabled => value.IsBitSet(5);
    public bool BackgroundAndWindowUsesLowTileDataArea => value.IsBitSet(4);
    public bool BackgroundUsesHighTileMapArea => value.IsBitSet(3);
    public bool IsDoubleSpriteSize => value.IsBitSet(2);
    public bool IsSpritesEnabled => value.IsBitSet(1);
    public bool IsBackgroundEnabled => value.IsBitSet(0);

    public static implicit operator byte(LcdControl lcdc) => lcdc.value;
    public static implicit operator LcdControl(byte value) => new(value);
}
