using System;
using GB_Emulator.Gameboi;
using GB_Emulator.Gameboi.Hardware;
using GB_Emulator.Sound.channels;
using NAudio.Wave;
using static GB_Emulator.Statics.SoundRegisters;
using static GB_Emulator.Statics.WavSettings;

namespace GB_Emulator.Sound
{
    public class SPU : Hardware, ILoop
    {
        private readonly NR50 nr50 = new();
        private readonly NR51 nr51 = new();
        private NR52 nr52;

        private Channel1 channel1;
        private Channel2 channel2;
        private Channel3 channel3;
        private Channel4 channel4;

        private readonly WaveOut waveEmitter = new();

        private readonly BufferedWaveProvider waveProvider;
        private readonly WaveFormat waveFormat;

        private const float samplesPerMillisecond = SAMPLE_RATE / 1000f;
        public float MillisecondsPerLoop { get; set; } = 1;


        private double masterVolume = 1d;
        private bool isMuted = false;
        private const double masterVolumeIncrement = .1d;

        public SPU()
        {
            waveFormat = new WaveFormat();
            waveProvider = new BufferedWaveProvider(waveFormat)
            {
                BufferLength = waveFormat.BlockAlign * waveFormat.SampleRate,
                DiscardOnBufferOverflow = true
            };
        }

        public override void Connect(Bus bus)
        {
            this.bus = bus;
            nr52 = new(bus);

            bus.ReplaceMemory(NR50_address, nr50);
            bus.ReplaceMemory(NR51_address, nr51);
            bus.ReplaceMemory(NR52_address, nr52);

            channel1 = new Channel1(nr52);
            channel2 = new Channel2(nr52);
            channel3 = new Channel3(nr52);
            channel4 = new Channel4(nr52);

            channel1.Connect(bus);
            channel2.Connect(bus);
            channel3.Connect(bus);
            channel4.Connect(bus);

            waveEmitter.Init(waveProvider);
            waveEmitter.Play();
        }

        public void VolumeUp()
        {
            masterVolume += masterVolumeIncrement;
            if (masterVolume > 1)
            {
                masterVolume = 1d;
            }
            if (isMuted) ToggleMute();
        }

        public void VolumeDown()
        {
            masterVolume -= masterVolumeIncrement;
            if (masterVolume < 0)
            {
                masterVolume = 0d;
            }
            if (isMuted) ToggleMute();
        }

        public void ToggleMute()
        {
            isMuted = !isMuted;
        }

        public Action<long> Loop => AddNextSamples;

        private long samplesAdded = 0;

        public void AddNextSamples(long currentMilliseconds)
        {
            var samplesAfter = (long)(currentMilliseconds * samplesPerMillisecond);
            var samplesToAdd = samplesAfter - samplesAdded;
            samplesAdded = samplesAfter;
            if (samplesToAdd > 0)
                AddNextSampleBatch((int)samplesToAdd);
        }

        private void AddNextSampleBatch(int sampleCount)
        {
            if (isMuted) return;
            var channel1Samples = nr52.IsAllOn || nr52.IsSoundOn(0) ? channel1.GetNextSampleBatch(sampleCount) : new short[sampleCount];
            var channel2Samples = nr52.IsAllOn || nr52.IsSoundOn(1) ? channel2.GetNextSampleBatch(sampleCount) : new short[sampleCount];
            var channel3Samples = nr52.IsAllOn || nr52.IsSoundOn(2) ? channel3.GetNextSampleBatch(sampleCount) : new short[sampleCount];
            var channel4Samples = nr52.IsAllOn || nr52.IsSoundOn(3) ? channel4.GetNextSampleBatch(sampleCount) : new short[sampleCount];

            var samples = new byte[sampleCount * 4];

            var out1volume = nr50.VolumeOut1;
            var out2volume = nr50.VolumeOut2;

            int index = 0;
            for (int i = 0; i < sampleCount; i++)
            {
                //channel1
                short c1Sample = 0;

                if (nr51.Is1Out1)
                    c1Sample += (short)(channel1Samples[i] / 4);
                if (nr51.Is2Out1)
                    c1Sample += (short)(channel2Samples[i] / 4);
                if (nr51.Is3Out1)
                    c1Sample += (short)(channel3Samples[i] / 4);
                if (nr51.Is4Out1)
                    c1Sample += (short)(channel4Samples[i] / 4);

                c1Sample = (short)(c1Sample * out1volume * masterVolume);

                samples[index++] = (byte)(c1Sample >> 8);
                samples[index++] = (byte)c1Sample;

                //channel2
                short c2Sample = 0;

                if (nr51.Is1Out2)
                    c2Sample += (short)(channel1Samples[i] / 4);
                if (nr51.Is2Out2)
                    c2Sample += (short)(channel2Samples[i] / 4);
                if (nr51.Is3Out2)
                    c2Sample += (short)(channel3Samples[i] / 4);
                if (nr51.Is4Out2)
                    c2Sample += (short)(channel4Samples[i] / 4);

                c2Sample = (short)(c2Sample * out2volume * masterVolume);

                samples[index++] = (byte)(c2Sample >> 8);
                samples[index++] = (byte)c2Sample;
            }

            waveProvider.AddSamples(samples, 0, samples.Length);
        }
    }
}