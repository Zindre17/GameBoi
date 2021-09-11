using GB_Emulator.Gameboi.Memory;
using GB_Emulator.Gameboi.Memory.Specials;
using static GB_Emulator.Statics.Frequencies;
using static GB_Emulator.Statics.TimerAddresses;

namespace GB_Emulator.Gameboi.Hardware
{
    public class Timer : Hardware, IUpdateable
    {

        private readonly TAC tac = new();
        private readonly DIV div = new();
        private readonly Register tma = new();
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
            tima.Write(tma.Read());
            bus.RequestInterrupt(InterruptType.Timer);
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
}