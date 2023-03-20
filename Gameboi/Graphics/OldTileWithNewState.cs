using System;
using Gameboi.Extensions;
using static Gameboi.Statics.TileDataConstants;
using Byte = Gameboi.Memory.Byte;

namespace Gameboi.Graphics;

public class OldTileWithNewState
{
    private readonly SystemState state;

    private readonly int startIndex;
    public OldTileWithNewState(int nr, SystemState state)
    {
        this.state = state;
        startIndex = nr * bytesPerTile;
    }

    private byte[] Data => state.VideoRam[startIndex..(startIndex + bytesPerTile)];

    public byte GetColorCode(Byte x, Byte y)
    {
        if (x > 7)
            throw new ArgumentOutOfRangeException(nameof(x), "x must be between 0 and 7");
        if (y > 7)
            throw new ArgumentOutOfRangeException(nameof(y), "y must be between 0 and 7");

        Byte result = 0;

        Byte rowIndex = y * 2;
        if (Data[rowIndex + 1].IsBitSet(7 - x))
        {
            result |= 2;
        }
        if (Data[rowIndex].IsBitSet(7 - x))
        {
            result |= 1;
        }

        return result;
    }
}

