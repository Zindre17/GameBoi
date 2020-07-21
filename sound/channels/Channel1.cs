using static SoundRegisters;

class Channel1 : SquareWaveChannel
{
    public Channel1(NR52 nr52) : base(nr52, 0, true)
    {
        sweep.Write(0x80);
        waveDuty.Write(0xBF);
        envelope.Write(0xF3);
        frequencyHigh.Write(0xBF);

        sweep.OverflowListeners += OnSweepOverflow;
    }

    private void OnSweepOverflow()
    {
        nr52.TurnAllOff();
    }

    public override void Connect(Bus bus)
    {
        this.bus = bus;

        bus.ReplaceMemory(NR10_address, sweep);
        bus.ReplaceMemory(NR11_address, waveDuty);
        bus.ReplaceMemory(NR12_address, envelope);
        bus.ReplaceMemory(NR13_address, frequencyLow);
        bus.ReplaceMemory(NR14_address, frequencyHigh);
    }
}