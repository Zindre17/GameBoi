using System;
using static Gameboi.Statics.WavSettings;

namespace Gameboi.Sound
{
    public class SquareWaveProvider
    {
        public int lowToHigh = 1;
        public int samplesPerPeriod = 2;

        private int sampleCounter = 0;

        public void UpdateSound(uint frequency, double duty)
        {
            samplesPerPeriod = Math.Max((int)(SAMPLE_RATE / frequency), 2);
            lowToHigh = (int)(samplesPerPeriod * duty);
        }

        public int GetSample()
        {
            sampleCounter++;
            sampleCounter %= samplesPerPeriod;
            return (short)(sampleCounter > lowToHigh ? 1 : 0);
        }

    }
}
