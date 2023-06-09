using Gameboi.Extensions;

namespace Gameboi.Io;

public readonly struct Tac
{
    private readonly byte value;

    public Tac(byte value) => this.value = value;

    public bool IsTimerEnabled => value.IsBitSet(2);
    public int TimerSpeedSelect => value & 3;

    public static implicit operator byte(Tac tac) => tac.value;
    public static implicit operator Tac(byte value) => new(value);
}
