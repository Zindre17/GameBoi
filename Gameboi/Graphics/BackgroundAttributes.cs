using Gameboi.Extensions;

namespace Gameboi.Graphics;

public readonly struct BackgroundAttributes
{
    private readonly byte data;
    public BackgroundAttributes(byte data) => this.data = data;

    public int PalletNr => data & 7;
    public int VramBankNr => (data >> 3) & 1;
    public bool IsHorizontallyFlipped => data.IsBitSet(5);
    public bool IsVerticallyFlipped => data.IsBitSet(6);
    public bool Priority => data.IsBitSet(7); // true => BG has priority | false => Use OAM priorty bit
}
