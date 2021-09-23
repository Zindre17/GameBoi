using System;
using GB_Emulator.Gameboi;
using GB_Emulator.Gameboi.Memory;
using static GB_Emulator.Statics.SoundRegisters;
using static GB_Emulator.Statics.WavSettings;
using Byte = GB_Emulator.Gameboi.Memory.Byte;

namespace GB_Emulator.Sound.channels
{
    public class Channel3 : SoundChannel
    {
        private readonly Register state = new MaskedRegister(0x7F);
        private readonly Register soundLength = new();
        private readonly Register outputLevel = new MaskedRegister(0x9F);
        private readonly FrequencyLow frequencyLow = new();
        private readonly FrequencyHigh frequencyHigh = new();

        private readonly WaveRam waveRam = new();
        public Channel3(NR52 nr52) : base(nr52) { }
        public override void Connect(Bus bus)
        {
            this.bus = bus;

            bus.ReplaceMemory(NR30_address, state);
            bus.ReplaceMemory(NR31_address, soundLength);
            bus.ReplaceMemory(NR32_address, outputLevel);
            bus.ReplaceMemory(NR33_address, frequencyLow);
            bus.ReplaceMemory(NR34_address, frequencyHigh);

            bus.RouteMemory(WaveRam_address_start, waveRam, WaveRam_address_end);
        }

        private int currentFrequency = 1;
        private long currentLength;
        private long samplesThisLength;
        private bool isStopped = true;
        public void Update()
        {
            if (frequencyHigh.IsInitial)
            {
                nr52.TurnOn(2);
                frequencyHigh.IsInitial = false;
                if (frequencyHigh.HasDuration)
                    currentLength = GetLengthInSamples();
                else
                    currentLength = -1;
                samplesThisLength = 0;
                currentFrequency = GetFrequency();
                isStopped = false;
            }
            else
            {
                if (!GetState())
                    isStopped = true;

                var newFrequency = GetFrequency();
                if (currentFrequency != newFrequency)
                    currentFrequency = newFrequency;
            }
        }

        private bool GetState() => state.Read()[7];
        private long GetLengthInSamples()
        {
            var seconds = (0x100 - soundLength.Read()) * (1d / 0x100);
            return (long)(seconds * SAMPLE_RATE);
        }
        private int GetFrequency()
        {
            Byte low = frequencyLow.LowBits;
            Byte high = frequencyHigh.HighBits;
            Address fdata = high << 8 | low;
            return 0x10000 / (0x800 - fdata);
        }

        private int GetVolumeShift()
        {
            Byte volumeData = (outputLevel.Read() & 0x60) >> 5;
            // 0: Mute
            // 1: 100% (no shift)
            // 2: 50% (1 shift right)
            // 3: 25% (2 shifts right)
            if (volumeData == 0)
                return 8;
            return 2 - (3 - volumeData);
        }

        private int step = 0;
        private ulong sampleNr = 0;

        public short[] GetNextSampleBatch(int count)
        {
            Update();

            short[] samples = new short[count];

            if (!nr52.IsSoundOn(2) || isStopped)
                return samples;

            byte[] wavePattern = waveRam.GetSamples();

            double samplesPerStep = SAMPLE_RATE / (double)currentFrequency / wavePattern.Length;
            if (samplesPerStep == 0) samplesPerStep = 1;

            int volumeShift = GetVolumeShift();

            for (int i = 0; i < count; i++)
            {
                if (currentLength != -1 && i + samplesThisLength >= currentLength)
                {
                    nr52.TurnOff(2);
                    return samples;
                }
                else
                {
                    step = (int)(sampleNr++ / samplesPerStep);
                    step %= wavePattern.Length;
                    var data = wavePattern[step];
                    samples[i] = (short)(data >> volumeShift);
                }
            }
            sampleNr %= (ulong)Math.Max(samplesPerStep * wavePattern.Length, 1);
            samplesThisLength += count;
            return samples;
        }
    }
}