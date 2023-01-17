using GB_Emulator.Memory;

namespace GB_Emulator.Sound
{
    public class Sweep : MaskedRegister
    {
        public Sweep() : base(0x80) { }

        private Byte SweepTime => (data & 0x70) >> 4;
        private bool IsSubtraction => data[3];
        private Byte NrSweepShift => data & 7;

        public delegate void OnSweepOverflow();
        public OnSweepOverflow OverflowListeners;

        public Address GetFrequencyAfterSweep(Address frequencyData, int cyclesElapsed)
        {
            int result = frequencyData;
            if (IsActive)
            {
                var secondsPerStep = SweepTime / 128d;
                var cyclesPerStep = secondsPerStep * Statics.Frequencies.cpuSpeed;

                var divisor = 1 << NrSweepShift;

                var steps = cyclesElapsed / cyclesPerStep;
                for (int i = 0; i < steps; i++)
                {
                    if (IsSubtraction)
                    {
                        var prev = result;
                        result -= result / divisor;
                        if (result < 0)
                        {
                            result = prev;
                            break;
                        }
                    }
                    else
                    {
                        result += result / divisor;
                        if (result >= 0x7FF)
                        {
                            OverflowListeners?.Invoke();
                            break;
                        }
                    }
                }
            }

            return result & 0x7FF;
        }

        private bool IsActive => SweepTime != 0 && NrSweepShift != 0;
        public ushort GetFrequencyDataChange(Address frequencyData, int times, byte sweepShifts, bool isSubtraction)
        {
            if (NrSweepShift == 0) return frequencyData;

            Address result = frequencyData;

            if (isSubtraction)
            {
                for (int i = 0; i < times; i++)
                {
                    Address shift = result / (1 << sweepShifts);
                    result -= shift;
                }
            }
            else
                for (int i = 0; i < times; i++)
                {
                    Address shift = result / (1 << sweepShifts);
                    result += shift;
                }

            if (result >= 0x7FF)
            {
                if (IsSubtraction)
                    result = 0;
                else
                    OverflowListeners?.Invoke();
            }

            return result;
        }

    }
}
