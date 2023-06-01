using Gameboi.Extensions;

namespace Gameboi.Sound;

public class SimpleNr43
{
    private readonly byte data;

    public SimpleNr43(byte data) => this.data = data;

    public int ClockShift => data & 7;
    public bool IsWidth7 => data.IsBitSet(3);
    public double ClockDivider => (data >> 4) & 0xf;
}
