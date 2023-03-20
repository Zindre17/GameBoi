using Gameboi.Extensions;
using static Gameboi.Statics.TileDataConstants;

namespace Gameboi.Graphics;

public class OldTileDataWithNewState
{
    private readonly OldTileWithNewState[] tiles = new OldTileWithNewState[tileCount];

    public OldTileDataWithNewState(SystemState state)
    {
        for (int i = 0; i < tileCount; i++)
            tiles[i] = new OldTileWithNewState(i, state);
    }

    //dataSelect => false: 0x8800 - 0x97FF | true: 0x8000 - 0x8FFF
    public OldTileWithNewState LoadTilePattern(byte patternIndex, bool dataSelect)
    {
        int index = patternIndex;

        if (!dataSelect && !patternIndex.IsBitSet(7))
        {
            index += startIndexOfSecondTable;
        }

        return tiles[index];
    }
}

