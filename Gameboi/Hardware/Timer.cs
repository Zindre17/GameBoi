using static TimerAddresses;
using static Frequencies;

class Timer : Hardware
{

    private TAC tac = new TAC();
    private DIV div = new DIV();
    private Register tma = new Register();
    private TIMA tima;

    public Timer() => tima = new TIMA(TimaOverflow);

    private ulong prevCpuCycle = 0;
    private ulong cyclesSinceLastDivTick = 0;
    private ulong cyclesSinceLastTimerTick = 0;
    public void Tick(ulong cpuCycle)
    {
        ulong elapsedCycles = cpuCycle - prevCpuCycle;
        cyclesSinceLastDivTick += elapsedCycles;

        while (cyclesSinceLastDivTick >= cpuToDivRatio)
        {
            div.Tick();
            cyclesSinceLastDivTick -= cpuToDivRatio;
        }

        if (tac.IsStarted)
        {
            uint ratio = cpuToTimerRatio[tac.TimerSpeed];
            cyclesSinceLastTimerTick += elapsedCycles;
            while (cyclesSinceLastTimerTick >= ratio)
            {
                tima.Tick();
                cyclesSinceLastTimerTick -= ratio;
            }
        }
        prevCpuCycle = cpuCycle;
    }

    private void TimaOverflow()
    {
        bus.RequestInterrrupt(InterruptType.Timer);
        tima.Write(tma.Read());
    }

    public override void Connect(Bus bus)
    {
        base.Connect(bus);
        bus.ReplaceMemory(TAC_address, tac);
        bus.ReplaceMemory(DIV_address, div);
        bus.ReplaceMemory(TMA_address, tma);
        bus.ReplaceMemory(TIMA_address, tima);
    }
}