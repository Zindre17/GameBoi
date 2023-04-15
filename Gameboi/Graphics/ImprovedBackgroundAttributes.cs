using Gameboi.Extensions;

namespace Gameboi.Graphics;

public readonly struct ImprovedBackgroundAttributes
{
    private readonly byte data;
    public ImprovedBackgroundAttributes(byte data) => this.data = data;

    public int PalletNr => data & 7;
    public int VramBankNr => (data >> 3) & 1;
    public bool IsHorizontallyFlipped => data.IsBitSet(5);
    public bool IsVerticallyFlipped => data.IsBitSet(6);
    public bool Priority => data.IsBitSet(7); // true => BG has priority | false => Use OAM priorty bit
}
