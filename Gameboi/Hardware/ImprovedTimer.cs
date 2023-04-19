using Gameboi.Extensions;
using Gameboi.Memory.Io;
using Gameboi.Memory.Specials;

namespace Gameboi.Hardware;

public class ImprovedTimer : IClocked
{
    private readonly SystemState state;

    public ImprovedTimer(SystemState state) => this.state = state;

    public void Tick()
    {
        var tac = new Tac(state.Tac);
        if (!tac.IsTimerEnabled)
        {
            return;
        }

        if (state.TicksUntilTimerInterrupt > 0)
        {
            state.TicksUntilTimerInterrupt -= 1;
            if (state.TicksUntilTimerInterrupt is 0)
            {
                var interruptRequests = new InterruptState(state.InterruptFlags);
                state.InterruptFlags = interruptRequests.WithTimerSet();
                state.Tima = state.NextTima;
            }
        }

        var preTick = state.TimerCounter;
        var postTick = (ushort)(preTick + 1);
        if (IsMultiplexerHigh(tac.TimerSpeedSelect, preTick)
            && IsMultiplexerLow(tac.TimerSpeedSelect, postTick))
        {
            IncrementTima(state);
        }

        state.TimerCounter = postTick;
    }

    public static void IncrementTima(SystemState state)
    {
        if (state.Tima is AboutToOverflow)
        {
            state.Tima = 0;
            state.NextTima = state.Tma;
            state.TicksUntilTimerInterrupt = 4;

            return;
        }
        state.Tima++;
    }

    public static bool IsMultiplexerHigh(int timerSpeedSelect, ushort timerCounter)
    {
        return timerSpeedSelect switch
        {
            0 => timerCounter.GetHighByte().IsBitSet(1),
            1 => timerCounter.GetLowByte().IsBitSet(3),
            2 => timerCounter.GetLowByte().IsBitSet(5),
            3 => timerCounter.GetLowByte().IsBitSet(7),
            _ => false,
        };
    }

    public static bool IsMultiplexerLow(int timerSpeedSelect, ushort timerCounter)
    {
        return !IsMultiplexerHigh(timerSpeedSelect, timerCounter);
    }

    private const byte AboutToOverflow = 0xff;
}
