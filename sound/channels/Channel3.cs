using static SoundRegisters;

class Channel3 : SoundChannel
{
    private Register state = new MaskedRegister(0x7F);
    private Register soundLength = new Register();
    private Register outputLevel = new MaskedRegister(0x9F);
    private FrequencyLow frequencyLow = new FrequencyLow();
    private FrequencyHigh frequencyHigh = new FrequencyHigh();

    private WaveRam waveRam = new WaveRam();

    public override void Connect(Bus bus)
    {
        this.bus = bus;

        bus.ReplaceMemory(NR30_address, state);
        bus.ReplaceMemory(NR31_address, soundLength);
        bus.ReplaceMemory(NR32_address, outputLevel);
        bus.ReplaceMemory(NR33_address, frequencyLow);
        bus.ReplaceMemory(NR34_address, frequencyHigh);
    }

    private int currentFrequency;

    public override void Tick()
    {
        var newFrequency = GetFrequency();
        if (currentFrequency == newFrequency) return;

    }

    private int GetFrequency()
    {
        var low = frequencyLow.Read();
        var high = frequencyHigh.HighBits;
        var fdata = high << 8 | low;
        return 0x10000 / (0x800 - fdata);
    }

    public short[] GetNextSampleBatch(int count)
    {
        short[] samples = new short[count];

        return samples;
    }
}