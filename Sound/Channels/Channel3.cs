using GB_Emulator.Gameboi;
using GB_Emulator.Gameboi.Memory;
using static GB_Emulator.Statics.SoundRegisters;
using static GB_Emulator.Statics.WavSettings;

namespace GB_Emulator.Sound.channels
{
    public class Channel3 : SoundChannel
    {
        private readonly Register state = new MaskedRegister(0x7F);
        private readonly Register soundLength = new ReadMaskedRegister(0xFF, 0);
        private readonly Register outputLevel = new MaskedRegister(0x9F);
        private readonly FrequencyLow frequencyLow = new();
        private readonly FrequencyHigh frequencyHigh = new();

        private readonly WaveRam waveRam = new();
        public Channel3(NR52 nr52) : base(nr52, 2)
        {
            mode = frequencyHigh;
        }
        public override void Connect(Bus bus)
        {
            base.Connect(bus);

            bus.ReplaceMemory(NR30_address, state);
            bus.ReplaceMemory(NR31_address, soundLength);
            bus.ReplaceMemory(NR32_address, outputLevel);
            bus.ReplaceMemory(NR33_address, frequencyLow);
            bus.ReplaceMemory(NR34_address, frequencyHigh);
            bus.RouteMemory(NR52_address + 1, new DummyRange(), WaveRam_address_start);
            bus.RouteMemory(WaveRam_address_start, waveRam, WaveRam_address_end);
        }

        protected override void OnInit()
        {
            sampleNr = 0;
        }

        private bool IsPlaying() => state.Read()[7];
        protected override int GetDurationInCycles()
        {
            var seconds = (0x100 - soundLength.Read()) * (1d / 0x100);
            return (int)(seconds * Statics.Frequencies.cpuSpeed);
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

        private int sampleNr = 0;

        public short[] GetNextSampleBatch(int count)
        {
            var samples = new short[count];

            if (!nr52.IsSoundOn(2) || !IsPlaying())
                return samples;

            var currentFrequency = GetFrequency();
            var volumeShift = GetVolumeShift();

            double samplesPerWaveRamSample = SAMPLE_RATE / (double)currentFrequency / (waveRam.Size * 2);

            for (int i = 0; i < count; i++)
            {
                var data = waveRam.GetSample((int)(sampleNr++ / samplesPerWaveRamSample));
                var volumeAdjustedSample = (short)(data >> volumeShift);
                samples[i] = volumeAdjustedSample;
            }
            return samples;
        }
    }
}