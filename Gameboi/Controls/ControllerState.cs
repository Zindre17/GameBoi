using Gameboi.Extensions;

namespace Gameboi.Controls;

// AKA: P1
public readonly struct ControllerState
{
    private readonly byte value;

    public ControllerState(byte value) => this.value = value;

    public bool IsInButtonMode => !value.IsBitSet(5);
    public bool IsInPadMode => !value.IsBitSet(4);

    public byte CurrentPresses => (byte)(value & 0xf);
}
