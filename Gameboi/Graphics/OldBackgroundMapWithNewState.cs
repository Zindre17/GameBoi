using static Gameboi.Statics.TileMapConstants;

namespace Gameboi.Graphics;

public class OldBackgroundMapWithNewState
{
    private readonly SystemState state;
    public OldBackgroundMapWithNewState(SystemState state)
    {
        this.state = state;
    }

    //mapSelect: false => 0x9800 - 0x9BFF | true => 0x9C00 - 0x9FFF
    public byte GetTilePatternIndex(byte x, byte y, bool mapSelect)
    {
        int index = y * tileMapWidth + x;

        if (mapSelect)
            index += tileMapSize;

        return state.VideoRam[0x1800 + index];
    }
}

