using System;
using static GB_Emulator.Statics.WavSettings;

namespace GB_Emulator.Sound
{
    public class SquareWaveProvider
    {
        private bool isStopped = true;
        public int lowToHigh;
        public int durationInSamples = -1;
        public int samplesPerPeriod;

        public void UpdateSound(uint frequency, double duty, bool isInitial, int duration = -1)
        {
            durationInSamples = isInitial ? duration : durationInSamples;
            samplesPerPeriod = Math.Max((int)(SAMPLE_RATE / frequency), 2);
            lowToHigh = (int)(samplesPerPeriod * duty);

            if (isInitial)
            {
                isStopped = false;
            }
        }

        private bool HasDuration => durationInSamples != -1;

        public delegate void DurationCompleteAction();
        public DurationCompleteAction OnDurationCompleted;

        public int GetSample(int sampleNr)
        {
            if (HasDuration && sampleNr >= durationInSamples)
            {
                isStopped = true;
                OnDurationCompleted?.Invoke();
            }

            short sample;

            var samplePoint = sampleNr % samplesPerPeriod;

            if (isStopped)
                sample = 0;
            else
                sample = (short)(samplePoint > lowToHigh ? 1 : 0);

            return sample;
        }

    }
}