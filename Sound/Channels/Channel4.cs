using System;
using GB_Emulator.Gameboi;
using GB_Emulator.Gameboi.Memory;
using static GB_Emulator.Statics.SoundRegisters;
using static GB_Emulator.Statics.WavSettings;
using Byte = GB_Emulator.Gameboi.Memory.Byte;

namespace GB_Emulator.Sound.channels
{
    public class Channel4 : SoundChannel
    {

        private readonly Envelope envelope = new();
        private readonly Register soundLength = new ReadMaskedRegister(0xFF, 0);
        private readonly NR43 nr43 = new();

        private readonly LFSR lfsr7 = new(7);
        private readonly LFSR lfsr15 = new(15);
        private LFSR currentLfsr;

        public Channel4(NR52 nr52) : base(nr52, 3)
        {
            mode = new ModeRegister();
            currentLfsr = lfsr7;
        }

        protected override int GetDurationInCycles()
        {
            Byte lengthData = soundLength.Read() & 0x3F;
            double seconds = (64 - lengthData) / 256d;
            return (int)(seconds * Statics.Frequencies.cpuSpeed);
        }

        private int CalculateFrequency()
        {
            var polyReg = nr43.Read();
            double r = polyReg & 7;
            if (r == 0)
            {
                r = .5;
            }
            var s = (polyReg & 0xF0) >> 4;
            return Math.Max(1, (int)(0x80000 / r / (2 << s)));
        }

        public override void Connect(Bus bus)
        {
            base.Connect(bus);

            bus.RouteMemory(NR41_address - 1, new Dummy());
            bus.ReplaceMemory(NR41_address, soundLength);
            bus.ReplaceMemory(NR42_address, envelope);
            bus.ReplaceMemory(NR43_address, nr43);
            bus.ReplaceMemory(NR44_address, mode);
        }

        private long sampleNr;
        private short signal = 0;
        public short[] GetNextSampleBatch(int count)
        {
            short[] samples = new short[count];

            if (!nr52.IsSoundOn(3))
                return samples;

            currentLfsr = nr43.GetStepsSelector() ? lfsr7 : lfsr15;

            var samplesPerShift = SAMPLE_RATE / CalculateFrequency();
            if (samplesPerShift == 0) samplesPerShift = 1;

            var volume = envelope.GetVolume(elapsedDurationInCycles);

            for (int i = 0; i < count; i++)
            {
                var shifts = sampleNr / samplesPerShift;
                while (shifts > 1)
                {
                    sampleNr -= (int)samplesPerShift;
                    shifts--;
                    signal = (short)(currentLfsr.Tick() ? 1 : 0);
                }

                samples[i] = (short)(signal * volume);

                sampleNr++;
            }

            return samples;
        }

        protected override void OnInit()
        {
            envelope.Initialize();
            currentLfsr.Reset();
            sampleNr = 0;
        }
    }
}