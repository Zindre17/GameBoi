using System;
using static WavSettings;

class Channel4 : SoundChannel
{

    private Envelope envelope = new Envelope();
    private Register soundLength = new MaskedRegister(0xC0);
    private Register polynomial = new Register();
    private Register mode = new MaskedRegister(0x3F);

    public Channel4()
    {
    }

    private long GetLengthInSamples()
    {
        var lengthData = soundLength.Read() & 0x3F;
        var seconds = (64 - lengthData) * (1 / 256);
        return seconds * SAMPLE_RATE;
    }

    private int CalculateFrequency(byte r, byte s)
    {
        return 0x80000 / r / (1 << (s + 1));
    }

    public override void Connect(Bus bus)
    {
        throw new System.NotImplementedException();
    }

    private long samplesThisDuration;
    private Random random = new Random();
    public short[] GetNextSampleBatch(int count)
    {
        short[] result = new short[count];
        bool isStopped = false;
        var volume = envelope.GetVolume();

        if (mode.Read()[6] && samplesThisDuration >= GetLengthInSamples())
            isStopped = true;

        for (int i = 0; i < result.Length; i++)
        {
            short sample;
            if (isStopped) sample = 0;
            else
            {
                int signal;
                do
                {
                    signal = random.Next(-1, 1);
                }
                while (signal == 0);
                sample = (short)(volume * signal);
            }
            result[i] = sample;
        }

        sampleNr += (uint)count;
        samplesThisDuration += count;

        return result;
    }

    public override void Tick()
    {
        var polyReg = polynomial.Read();
        Byte r = polyReg & 7;
        Byte s = (polyReg & 0xF0) >> 4;
        var newFreq = CalculateFrequency(r, s);

    }
}