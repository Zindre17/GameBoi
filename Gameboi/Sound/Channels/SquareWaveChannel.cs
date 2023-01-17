using Gameboi.Memory;

namespace Gameboi.Sound.channels
{
    public abstract class SquareWaveChannel : SoundChannel
    {
        protected Sweep sweep;
        protected WaveDuty waveDuty = new();
        protected Envelope envelope = new();
        protected FrequencyLow frequencyLow = new();
        protected FrequencyHigh frequencyHigh = new();
        private readonly SquareWaveProvider waveProvider = new();

        public SquareWaveChannel(NR52 nr52, byte channelNr, bool hasSweep) : base(nr52, channelNr)
        {
            mode = frequencyHigh;
            if (hasSweep)
            {
                sweep = new Sweep();
                sweep.OverflowListeners += () => nr52.TurnOff(channelNr);
            }
        }

        public short[] GetNextSampleBatch(int count)
        {
            short[] samples = new short[count];
            if (!nr52.IsChannelOn(channelNr))
            {
                return samples;
            }

            var frequencyData = GetFrequencyData();
            if (sweep is not null)
            {
                frequencyData = sweep.GetFrequencyAfterSweep(frequencyData, elapsedDurationInCycles);
            }

            var frequency = GetFrequency(frequencyData);
            waveProvider.UpdateSound(frequency, waveDuty.GetDuty());

            volume = envelope.GetVolume(elapsedDurationInCycles);

            for (int i = 0; i < count; i++)
            {
                var sample = waveProvider.GetSample();
                samples[i] = (short)(sample * volume);
            }
            return samples;
        }

        private ushort GetFrequencyData()
        {
            return (ushort)((frequencyHigh.HighBits << 8) | frequencyLow.LowBits);
        }

        private static uint GetFrequency(ushort frequencyData)
        {
            return (uint)(0x20000 / (0x800 - frequencyData));
        }

        protected override int GetDurationInCycles()
        {
            return (int)(waveDuty.GetSoundLengthInSeconds() * Statics.Frequencies.cpuSpeed);
        }

        private Address volume = 0;

        protected override void OnInit()
        {
            envelope.Initialize();
        }
    }
}
