using Gameboi.Extensions;

namespace Gameboi.Sound;

public readonly struct SimpleNr51
{
    private readonly byte data;

    public SimpleNr51(byte data) => this.data = data;

    public bool Is1Out1 => data.IsBitSet(0);
    public bool Is2Out1 => data.IsBitSet(1);
    public bool Is3Out1 => data.IsBitSet(2);
    public bool Is4Out1 => data.IsBitSet(3);
    public bool Is1Out2 => data.IsBitSet(4);
    public bool Is2Out2 => data.IsBitSet(5);
    public bool Is3Out2 => data.IsBitSet(6);
    public bool Is4Out2 => data.IsBitSet(7);
}
