using static Frequencies;
using static TimerAddresses;

public class Timer : Hardware, IUpdateable
{

    private readonly TAC tac = new TAC();
    private readonly DIV div = new DIV();
    private readonly Register tma = new Register();
    private readonly TIMA tima;

    public Timer() => tima = new TIMA(TimaOverflow);

    private ulong cyclesSinceLastDivTick = 0;
    private ulong cyclesSinceLastTimerTick = 0;

    public void Update(byte cycles)
    {
        cyclesSinceLastDivTick += cycles;

        while (cyclesSinceLastDivTick >= cpuToDivRatio)
        {
            div.Bump();
            cyclesSinceLastDivTick -= cpuToDivRatio;
        }

        if (tac.IsStarted)
        {
            uint ratio = cpuToTimerRatio[tac.TimerSpeed];
            cyclesSinceLastTimerTick += cycles;
            while (cyclesSinceLastTimerTick >= ratio)
            {
                tima.Bump();
                cyclesSinceLastTimerTick -= ratio;
            }
        }
    }

    private void TimaOverflow()
    {
        bus.RequestInterrupt(InterruptType.Timer);
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