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
        // TODO: Add bugs/quirks of timer
        state.TicksSinceLastDivIncrement++;
        if (ShouldDivIncrement())
        {
            state.Div++;
            state.TicksSinceLastDivIncrement = 0;
        }

        var tac = new Tac(state.Tac);
        if (tac.IsTimerEnabled is false)
        {
            return;
        }

        state.TicksSinceLastTimaIncrement++;
        if (ShouldTimerIncrement(tac) is false)
        {
            return;
        }

        state.TicksSinceLastTimaIncrement = 0;
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
        => state.TicksSinceLastDivIncrement == ticksPerDivIncrement;

    private bool ShouldTimerIncrement(Tac tac)
    {
        var ticksPerIncrement = ticksPerIncrementPerTimerSpeed[tac.TimerSpeedSelect];
        return state.TicksSinceLastTimaIncrement >= ticksPerIncrement;
    }

    private const byte AboutToOverflow = 0xff;
}
