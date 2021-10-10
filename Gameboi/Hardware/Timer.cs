using GB_Emulator.Gameboi.Memory;
using GB_Emulator.Gameboi.Memory.Specials;
using static GB_Emulator.Statics.Frequencies;
using static GB_Emulator.Statics.TimerAddresses;

namespace GB_Emulator.Gameboi.Hardware
{
    public class Timer : Hardware, IUpdateable
    {

        private readonly TAC tac = new();
        private readonly DIV div;
        private readonly Register tma = new();
        private readonly TIMA tima;

        public Timer()
        {
            div = new DIV(ReloadTima);
            tima = new TIMA(TimaOverflow);
        }

        private ulong cyclesSinceLastDivTick = 0;
        private ulong cyclesSinceLastTimerTick = 0;

        private bool isOverflown = false;

        public void Update(byte cycles, ulong speed)
        {
            if (isOverflown)
            {
                ReloadTima();
            }
            cyclesSinceLastDivTick += cycles / speed;

            while (cyclesSinceLastDivTick >= cpuToDivRatio / speed)
            {
                div.Bump();
                cyclesSinceLastDivTick -= cpuToDivRatio / speed;
            }

            if (tac.IsStarted)
            {
                uint ratio = cpuToTimerRatio[tac.TimerSpeed];
                cyclesSinceLastTimerTick += cycles;
                while (cyclesSinceLastTimerTick >= ratio)
                {
                    tima.Bump();
                    cyclesSinceLastTimerTick -= ratio;
                    if (isOverflown && cyclesSinceLastTimerTick > 4)
                    {
                        ReloadTima();
                    }
                }
            }
        }

        private void ReloadTima()
        {
            isOverflown = false;
            tima.Write(tma.Read());
        }

        private void TimaOverflow()
        {
            bus.RequestInterrupt(InterruptType.Timer);
            isOverflown = true;
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