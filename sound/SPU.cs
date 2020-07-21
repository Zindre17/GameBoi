using static SoundRegisters;

class SPU : Hardware
{
    private NR52 nr52 = new NR52();

    private Channel1 channel1;
    private Channel2 channel2;

    public SPU()
    {
        channel1 = new Channel1(nr52);
        channel2 = new Channel2(nr52);
    }

    public void Tick(Byte cpuTick)
    {
        channel1.Tick(cpuTick);
        channel2.Tick(cpuTick);
    }

    public override void Connect(Bus bus)
    {
        this.bus = bus;

        bus.ReplaceMemory(NR52_address, nr52);

        channel1.Connect(bus);
        channel2.Connect(bus);
    }
}