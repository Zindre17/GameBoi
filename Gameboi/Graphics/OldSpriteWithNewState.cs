using Gameboi.Extensions;

namespace Gameboi.Graphics;

public class OldSpriteWithNewState
{
    private readonly SystemState state;
    private readonly byte nr;
    private readonly int startIndex;

    public byte Nr => nr;

    public OldSpriteWithNewState(byte nr, SystemState state) => (this.nr, this.state, startIndex) = (nr, state, nr * 4);


    public byte Y => state.Oam[startIndex + 0];
    public byte X => state.Oam[startIndex + 1];
    public byte Pattern => state.Oam[startIndex + 2];
    public bool Hidden => state.Oam[startIndex + 3].IsBitSet(7); // Other refer to it as "Priority" => 0: display on top, 1: hide under 1,2 and 3 of bg and
    public bool Yflip => state.Oam[startIndex + 3].IsBitSet(6);
    public bool Xflip => state.Oam[startIndex + 3].IsBitSet(5);
    public bool Palette => state.Oam[startIndex + 3].IsBitSet(4);
    public int VramBank => state.Oam[startIndex + 3].IsBitSet(3) ? 1 : 0;
    public byte ColorPalette => (byte)(state.Oam[startIndex + 3] & 7);

    public int ScreenYstart => Y - 16;
    public int ScreenXstart => X - 8;

    public bool IsWithinScreenWidth() => X > 0 && X < 168;
    public bool IsWithinScreenHeight() => Y > 0 && ScreenYstart < 144;
    public bool IsIntersectWithLine(byte line, bool doubleHeighMode = false)
    {
        int screenYend = ScreenYstart + (doubleHeighMode ? 16 : 8);
        return ScreenYstart <= line && line < screenYend;
    }
}

