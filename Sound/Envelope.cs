using System;
using GB_Emulator.Gameboi.Memory;
using Byte = GB_Emulator.Gameboi.Memory.Byte;

namespace GB_Emulator.Sound
{
    public class Envelope : Register
    {

        public void Initialize()
        {
            initialVolume = data >> 4;
            isIncrease = data[3];
            Byte stepLengthData = data & 7;

            if (stepLengthData == 0 || (isIncrease && initialVolume == 0xF) || (!isIncrease && initialVolume == 0))
                isActive = false;
            else
            {
                var secondsPerStep = stepLengthData / 64d;
                cyclesPerStep = (int)(secondsPerStep * Statics.Frequencies.cpuSpeed);
                isActive = true;
            }
        }

        private Byte initialVolume;
        private int cyclesPerStep;
        private bool isActive;
        private bool isIncrease;

        public Address GetVolume(int elapsedCycles)
        {
            Byte currentVolume = initialVolume;
            if (isActive)
            {
                int steps = elapsedCycles / cyclesPerStep;
                if (isIncrease)
                    currentVolume = Math.Min(0x0F, initialVolume + steps);
                else
                    currentVolume = Math.Max(0, initialVolume - steps);
            }
            return ScaleVolume(currentVolume);
        }

        private static Address ScaleVolume(byte volume) => 1 * volume;
    }
}