using System;
using static WavSettings;
using static Frequencies;
using static SoundRegisters;

class Channel4 : SoundChannel
{

    private Envelope envelope = new Envelope();
    private Register soundLength = new MaskedRegister(0xC0);
    private NR43 nr43 = new NR43();
    private ModeRegister mode = new ModeRegister();

    private LFSR lfsr7 = new LFSR(7);
    private LFSR lfsr15 = new LFSR(15);
    private LFSR currentLfsr;

    public Channel4(NR52 nr52) : base(nr52) { }

    private long GetLengthInSamples()
    {
        Byte lengthData = soundLength.Read() & 0x3F;
        double seconds = (64 - lengthData) / 256d;
        return (long)(seconds * SAMPLE_RATE);
    }

    private double GetFrequency()
    {
        double sf = nr43.GetShiftFrequency();
        return SAMPLE_RATE / sf;
    }
    private int CalculateFrequency(double r, double s)
    {
        return Math.Max(1, (int)(SAMPLE_RATE / (0x80000 / r / (1 << (int)(s + 1)))));
    }

    public override void Connect(Bus bus)
    {
        this.bus = bus;

        bus.ReplaceMemory(NR41_address, soundLength);
        bus.ReplaceMemory(NR42_address, envelope);
        bus.ReplaceMemory(NR43_address, nr43);
        bus.ReplaceMemory(NR44_address, mode);
    }

    private int samplesThisDuration;
    private long sampleNr;
    private long currentDuration = -1;
    private double samplesPerShift = 1;
    private short signal = 0;
    public short[] GetNextSampleBatch(int count)
    {
        Update(0);
        short[] samples = new short[count];

        for (int i = 0; i < samples.Length; i++)
        {
            if (currentDuration != -1 && samplesThisDuration >= currentDuration)
            {
                nr52.TurnOff(3);
                break;
            }

            var shifts = sampleNr / samplesPerShift;
            while (shifts > 1)
            {
                signal = (short)(currentLfsr.Tick() ? 1 : -1);
                shifts--;
                sampleNr--;
            }

            var volume = envelope.GetVolume(samplesThisDuration++);
            samples[i] = (short)(signal * volume);

            sampleNr++;
        }

        return samples;
    }

    public void Update(byte cycles)
    {
        currentLfsr = nr43.GetStepsSelector() ? lfsr7 : lfsr15;
        // samplesPerShift = GetFrequency();

        if (mode.IsInitial)
        {
            nr52.TurnOn(3);
            mode.IsInitial = false;

            if (mode.HasDuration)
            {
                currentDuration = GetLengthInSamples();
            }
            else
            {
                currentDuration = -1;
            }

            envelope.Initialize();
            currentLfsr.Reset();
            samplesThisDuration = 0;
            sampleNr = 0;
        }

        var polyReg = nr43.Read();
        double r = polyReg & 7;
        if (r == 0) r = .5;
        double s = (polyReg & 0xF0) >> 4;
        samplesPerShift = CalculateFrequency(r, s);

    }
}