using Gameboi.Memory.Io;
using Gameboi.Memory.Specials;
using static Gameboi.Statics.Frequencies;

namespace Gameboi.Hardware;

public class ImprovedTimer : IClocked
{
    private readonly SystemState state;

    public ImprovedTimer(SystemState state) => this.state = state;

    public void Tick()
    {
        if (ShouldDivIncrement())
        {
            state.Div++;
        }

        var tac = new Tac(state.Tac);
        if (tac.IsTimerEnabled is false)
        {
            return;
        }

        if (ShouldTimerIncrement(tac) is false)
        {
            return;
        }

        if (state.Tima is AboutToOverflow)
        {
            state.Tima = state.Tma;
            var interruptRequests = new InterruptState(state.InterruptFlags);
            state.InterruptFlags = interruptRequests.WithTimerSet();
            return;
        }
        state.Tima++;
    }

    private bool ShouldDivIncrement()
        => (state.TicksElapsedThisFrame % ticksPerDivIncrement) is 0;

    private bool ShouldTimerIncrement(Tac tac)
    {
        var ticksPerIncrement = ticksPerIncrementPerTimerSpeed[tac.TimerSpeedSelect];
        return (state.TicksElapsedThisFrame % ticksPerIncrement) is 0;
    }

    private const byte AboutToOverflow = 0xff;
}
