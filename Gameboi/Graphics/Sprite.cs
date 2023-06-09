using Gameboi.Extensions;

namespace Gameboi.Graphics;

public readonly struct Sprite
{
    private const byte SpriteSizeInBytes = 4;

    private readonly byte[] oam;
    private readonly int startAddress;
    private readonly int spriteNr;

    public Sprite(byte[] oam, int spriteNr)
    {
        this.oam = oam;
        this.spriteNr = spriteNr;
        startAddress = spriteNr * SpriteSizeInBytes;
    }

    public int SpriteNr => spriteNr;

    public byte Y => oam[startAddress];
    public byte X => oam[startAddress + 1];

    public byte TileIndex => oam[startAddress + 2];

    public bool Hidden => oam[startAddress + 3].IsBitSet(7); // Other refer to it as "Priority" => 0: display on top, 1: hide under 1,2 and 3 of bg and
    public bool Yflip => oam[startAddress + 3].IsBitSet(6);
    public bool Xflip => oam[startAddress + 3].IsBitSet(5);
    public bool UsePalette1 => oam[startAddress + 3].IsBitSet(4);
    public int VramBank => oam[startAddress + 3].IsBitSet(3) ? 1 : 0;
    public byte ColorPalette => (byte)(oam[startAddress + 3] & 7);
}
