using static SoundRegisters;

class Channel3 : SoundChannel
{
    private Register state = new MaskedRegister(0x7F);
    private Register soundLength = new Register();
    private Register outputLevel = new MaskedRegister(0x9F);
    private FrequencyLow frequencyLow = new FrequencyLow();
    private FrequencyHigh frequencyHigh = new FrequencyHigh();

    // public Channel3() : base(null) { }

    public override void Connect(Bus bus)
    {
        this.bus = bus;

        bus.ReplaceMemory(NR30_address, state);
        bus.ReplaceMemory(NR31_address, soundLength);
        bus.ReplaceMemory(NR32_address, outputLevel);
        bus.ReplaceMemory(NR33_address, frequencyLow);
        bus.ReplaceMemory(NR34_address, frequencyHigh);
    }

    public override void Tick(Byte cpuCycles)
    {
        throw new System.NotImplementedException();
    }
}