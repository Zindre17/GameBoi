using GB_Emulator.Gameboi;

namespace GB_Emulator.Sound.channels
{
    public abstract class SquareWaveChannel : SoundChannel
    {
        protected Sweep sweep;
        protected WaveDuty waveDuty = new();
        protected Envelope envelope = new();
        protected FrequencyLow frequencyLow = new();
        protected FrequencyHigh frequencyHigh = new();

        protected byte channelBit;

        private readonly SquareWaveProvider waveProvider = new();

        public SquareWaveChannel(NR52 nr52, byte channelBit, bool hasSweep) : base(nr52)
        {
            this.channelBit = channelBit;

            if (hasSweep) sweep = new Sweep();

            waveProvider.OnDurationCompleted += () => nr52.TurnOff(channelBit);
        }

        private int samplesThisDuration = 0;
        public short[] GetNextSampleBatch(int count)
        {
            ushort frequencyData = GetFrequencyData();
            if (frequencyHigh.IsInitial)
            {
                frequencyHigh.IsInitial = false;
                nr52.TurnOn(channelBit);

                samplesThisDuration = 0;

                envelope.Initialize();

                int newDuration = frequencyHigh.HasDuration ? waveDuty.GetSoundLengthInSamples() : -1;

                waveProvider.UpdateSound(
                    GetFrequency(frequencyData),
                    waveDuty.GetDuty(),
                    true,
                    newDuration
                );
            }
            else
            {
                if (sweep != null)
                    frequencyData = sweep.GetFrequencyAfterSweep(frequencyData, samplesThisDuration);

                waveProvider.UpdateSound(
                    GetFrequency(frequencyData),
                    waveDuty.GetDuty(),
                    false
                );
            }

            short[] samples = new short[count];
            if (nr52.IsSoundOn(channelBit))
            {
                for (int i = 0; i < count; i++)
                {
                    var sample = waveProvider.GetSample(samplesThisDuration);
                    var volume = envelope.GetVolume(samplesThisDuration++);
                    samples[i] = (short)(sample * volume);
                }
            }

            return samples;
        }

        public abstract override void Connect(Bus bus);


        private ushort GetFrequencyData()
        {
            return (ushort)((frequencyHigh.HighBits << 8) | frequencyLow.LowBits);
        }

        private static uint GetFrequency(ushort frequencyData)
        {
            return (uint)(0x20000 / (0x800 - frequencyData));
        }

    }
}