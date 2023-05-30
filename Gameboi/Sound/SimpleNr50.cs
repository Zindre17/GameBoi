using Gameboi.Extensions;

namespace Gameboi.Sound;

public readonly struct SimpleNr50
{
    private readonly byte data;

    public SimpleNr50(byte data) => this.data = data;

    public bool IsVinOut1 => data.IsBitSet(3);
    public bool IsVinOut2 => data.IsBitSet(7);

    public int VolumeOut2 => (data & 0x70) >> 4;
    public int VolumeOut1 => data & 7;
}
