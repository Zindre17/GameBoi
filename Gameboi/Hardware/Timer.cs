using GB_Emulator.Gameboi.Memory;
using GB_Emulator.Gameboi.Memory.Specials;
using static GB_Emulator.Statics.TimerAddresses;

namespace GB_Emulator.Gameboi.Hardware
{
    public class Timer : Hardware, IUpdateable
    {

        private readonly DIV div;
        private readonly TIMA tima;
        private readonly Register tma = new();
        private readonly TAC tac;

        public Timer()
        {
            div = new DIV(OnDivWrite);
            tima = new TIMA(TimaOverflow);
            tac = new TAC(OnTimerDisabled, OnTimerSpeedChanged);
            counterAtNextBump = tac.GetNextCounter(0);
        }

        private bool isOverflown = false;
        private int counterAtNextBump;
        private bool isWatingForCounterReset = false;

        public void Update(byte cycles, ulong _)
        {
            if (isOverflown)
            {
                ReloadTima();
            }
            div.AddCycles(cycles);

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

        private void OnTimerSpeedChanged(TAC newTac)
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
            bus.RouteMemory(DIV_address, div);
            bus.ReplaceMemory(TMA_address, tma);
            bus.ReplaceMemory(TIMA_address, tima);
        }
    }
}