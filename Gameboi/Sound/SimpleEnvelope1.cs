using Gameboi.Extensions;

namespace Gameboi.Sound;

public readonly struct SimpleEnvelope
{
    private readonly byte value;

    public SimpleEnvelope(byte value) => this.value = value;

    public int InitialVolume => value >> 4;
    public bool IsIncrease => value.IsBitSet(3);
    public int StepLength => value & 7;

    public bool IsActive => StepLength > 0;
}
