using System;
using static GB_Emulator.Statics.ByteOperations;
using static GB_Emulator.Statics.Frequencies;

namespace GB_Emulator.Gameboi.Memory.Specials
{
    public class TAC : MaskedRegister
    {
        public TAC(Action onTimerDisabled = null, Action<TAC> onTimerSpeedChanged = null) : base(0xF8)
        {
            OnTimerDisabled = onTimerDisabled;
            OnTimerSpeedChanged = onTimerSpeedChanged;
        }

        public bool IsStarted => TestBit(2, data);

        public Byte TimerSpeed => data & 3;

        public Action OnTimerDisabled { get; private set; }
        public Action<TAC> OnTimerSpeedChanged { get; private set; }

        public int GetNextCounter(int current)
        {
            return current + (int)cpuToTimerRatio[TimerSpeed];
        }

        public bool IsTriggerBitSet(Address counter)
        {
            return (counter & cpuToTimerRatio[TimerSpeed]) > 0;
        }

        public override void Write(Byte value)
        {
            if (IsStarted && !value[2])
            {
                OnTimerDisabled?.Invoke();
            }
            if (TimerSpeed != (value & 3) && value[2])
            {
                var newTac = new TAC
                {
                    data = value
                };
                OnTimerSpeedChanged?.Invoke(newTac);
            }

            base.Write(value);

        }

    }
}