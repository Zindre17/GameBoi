using System;
using Gameboi.Extensions;
using static Gameboi.Statics.Frequencies;

namespace Gameboi.Memory.Specials;

public class OldTacWithNewState
{
    private readonly SystemState state;

    public OldTacWithNewState(SystemState state, Action? onTimerDisabled = null, Action<OldTacWithNewState>? onTimerSpeedChanged = null)
    {
        this.state = state;
        OnTimerDisabled = onTimerDisabled;
        OnTimerSpeedChanged = onTimerSpeedChanged;
    }

    public bool IsStarted => state.Tac.IsBitSet(2);

    public byte TimerSpeed => (byte)(state.Tac & 3);

    public Action? OnTimerDisabled { get; private set; }
    public Action<OldTacWithNewState>? OnTimerSpeedChanged { get; private set; }

    public int GetNextCounter(int current)
    {
        return current + (int)ticksPerIncrementPerTimerSpeed[TimerSpeed];
    }

    public bool IsTriggerBitSet(Address counter)
    {
        return (counter & ticksPerIncrementPerTimerSpeed[TimerSpeed]) > 0;
    }
}
