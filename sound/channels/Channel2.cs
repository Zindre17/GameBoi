using static SoundRegisters;

public class Channel2 : SquareWaveChannel
{
    public Channel2(NR52 nr52) : base(nr52, 1, false)
    {
    }

    public override void Connect(Bus bus)
    {
        this.bus = bus;

        bus.ReplaceMemory(NR21_address, waveDuty);
        bus.ReplaceMemory(NR22_address, envelope);
        bus.ReplaceMemory(NR23_address, frequencyLow);
        bus.ReplaceMemory(NR24_address, frequencyHigh);
    }

}