using static ByteOperations;

class Timer : Hardware<MainBus>
{
    private const ushort DIV_address = 0xFF04; // Divider register
    // DIV is incremented at 16384Hz = 0x4000Hz
    private const ushort TIMA_address = 0xFF05; // Timer counter
    private const ushort TMA_address = 0xFF06; // Timer modulo
    private const ushort TAC_address = 0xFF07; // Timer control
    // TAC speeds
    // bit 2 : 0 = Stop, 1 = Start
    // bit 1 - 0: 
    //      00 = 4096Hz = 0x1000Hz,
    //      01 = 262144Hz = 0x40000Hz, 
    //      10 = 65536Hz = 0x10000Hz, 
    //      11 = 16384Hz = 0x4000Hz  
    private static readonly uint[] speeds = new uint[4]{
        0x1000,
        0x40000,
        0x10000,
        0x4000
    };

    private static readonly uint cpuSpeed = 0x400000;

    private static readonly uint[] ratios = new uint[4]{
        cpuSpeed / speeds[0],
        cpuSpeed / speeds[1],
        cpuSpeed / speeds[2],
        cpuSpeed / speeds[3]
    };

    private static readonly uint divRatio = ratios[3];

    private byte Read(ushort address)
    {
        if (!bus.Read(address, out byte value))
            throw new MemoryReadException(address);
        return value;
    }

    private void Write(ushort address, byte value)
    {
        if (!bus.Write(address, value))
            throw new MemoryWriteException(address);
    }

    private ulong prevCpuCycle = 0;

    public void Clock(ulong cpuCycle)
    {
        ulong elapsedCycles = cpuCycle - prevCpuCycle;
        if (elapsedCycles >= divRatio)
            TickDIV();

        bool enabled = ReadTAC(out int mode);
        uint ratio = ratios[mode];
        if (enabled && elapsedCycles >= ratio)
        {
            TickTIMA();
        }
        prevCpuCycle = cpuCycle;
    }

    private bool ReadTAC(out int mode)
    {
        byte tac = Read(TAC_address);
        mode = tac & 0b11; // get first two bits
        return TestBit(2, tac);
    }

    private void TickDIV()
    {
        byte value = Read(DIV_address);
        value++;
        Write(DIV_address, value);
    }

    private void TickTIMA()
    {
        byte tima = Read(TIMA_address);
        tima++;
        if (tima == 0)
        {
            byte tma = Read(TMA_address);
            Write(TIMA_address, tma);
            bus.RequestInterrrupt(InterruptType.Timer);
        }
    }
}