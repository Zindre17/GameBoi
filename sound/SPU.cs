using NAudio.Wave;
using static SoundRegisters;

class SPU : Hardware, IWaveProvider
{
    private NR52 nr52 = new NR52();

    private Channel1 channel1;
    private Channel2 channel2;
    private readonly WaveOut waveEmitter = new WaveOut();

    public WaveFormat WaveFormat => new WaveFormat();

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

        waveEmitter.Init(this);
        waveEmitter.Play();
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        int bytesRead = 0;
        while (bytesRead < count)
        {
            short sample = 0;
            sample += channel1.GetNextSample();
            sample += channel2.GetNextSample();

            Byte high = sample >> 8;
            Byte low = sample;

            //channel1
            buffer[bytesRead++] = high;
            buffer[bytesRead++] = low;
            //channel2
            buffer[bytesRead++] = high;
            buffer[bytesRead++] = low;
        }

        return bytesRead;
    }
}