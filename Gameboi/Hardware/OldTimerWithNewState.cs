using Gameboi.Memory.Specials;

namespace Gameboi.Hardware;

public class OldTimerWithNewState
{

    private readonly OldDivWithNewState div;
    private readonly OldTimaWithNewState tima;
    private readonly OldTacWithNewState tac;
    private readonly SystemState state;

    public OldTimerWithNewState(SystemState state)
    {
        this.state = state;
        div = new OldDivWithNewState(state, OnDivWrite);
        tima = new OldTimaWithNewState(state, TimaOverflow);
        tac = new OldTacWithNewState(state, OnTimerDisabled, OnTimerSpeedChanged);
        counterAtNextBump = tac.GetNextCounter(0);
    }

    private bool isOverflown = false;
    private int counterAtNextBump;
    private bool isWatingForCounterReset = false;

    public void Tick()
    {
        if (isOverflown)
        {
            ReloadTima();
        }
        div.Tick();

        if (tac.IsStarted && !isWatingForCounterReset)
        {
            while (div.Counter >= counterAtNextBump)
            {
                tima.Bump();
                if (isOverflown && (div.Counter - counterAtNextBump) > 4)
                {
                    ReloadTima();
                }
                counterAtNextBump = tac.GetNextCounter(counterAtNextBump);
            }
            if (counterAtNextBump > 0xFFFF)
                isWatingForCounterReset = true; // next tick is after div overflows
        }
    }

    private void OnTimerSpeedChanged(OldTacWithNewState newTac)
    {
        if (newTac.IsStarted && newTac.IsTriggerBitSet(div.Counter) && !tac.IsTriggerBitSet(div.Counter))
        {
            tima.Bump();
        }
    }

    private void OnTimerDisabled()
    {
        if (tac.IsTriggerBitSet(div.Counter))
        {
            tima.Bump();
        }
    }

    private void OnDivWrite()
    {
        isWatingForCounterReset = false;
        var counter = div.Counter;
        if (tac.IsTriggerBitSet(counter))
        {
            tima.Bump();
        }
        counterAtNextBump = tac.GetNextCounter(0);
    }

    private void ReloadTima()
    {
        isOverflown = false;
        state.Tima = state.Tma;
    }

    private void TimaOverflow()
    {
        var interruptRequests = new InterruptState(state.InterruptFlags);
        state.InterruptFlags = interruptRequests.WithTimerSet();
        isOverflown = true;
    }
}
