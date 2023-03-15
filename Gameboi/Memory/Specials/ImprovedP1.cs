using Gameboi.Extensions;

namespace Gameboi.Memory.Specials;

public readonly struct ImprovedP1
{
    private readonly byte value;

    public ImprovedP1(byte value) => this.value = value;

    public bool P15 => !value.IsBitSet(5);
    public bool P14 => !value.IsBitSet(4);

    public byte CurrentPresses => (byte)(value & 0xf);
}
