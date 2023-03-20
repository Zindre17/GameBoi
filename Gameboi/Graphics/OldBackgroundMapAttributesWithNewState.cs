using Gameboi.Extensions;
using Gameboi.Memory;
using static Gameboi.Statics.TileMapConstants;

namespace Gameboi.Graphics;

class OldBackgroundAttributeWithNewState
{
    private readonly byte data;
    public OldBackgroundAttributeWithNewState(byte data) => this.data = data;

    public byte PalletNr => (byte)(data & 7);

    public byte VramBankNr => (byte)((data >> 3) & 1);

    public bool IsHorizontallyFlipped => data.IsBitSet(5);
    public bool IsVerticallyFlipped => data.IsBitSet(6);
    public bool Priority => data.IsBitSet(7); // true => BG has priority | false => Use OAM priorty bit
}

class OldBackgroundAttributeMapWithNewState
{
    private readonly SystemState state;

    public OldBackgroundAttributeMapWithNewState(SystemState state)
    {
        this.state = state;
    }

    public OldBackgroundAttributeWithNewState GetBackgroundAttributes(Byte x, Byte y, bool mapSelect)
    {
        int index = y * tileMapWidth + x;

        if (mapSelect)
            index += tileMapSize;

        return new(state.VideoRam[0x1800 + index]); // TODO fix: should use other vram bank (GBC)
    }
}

