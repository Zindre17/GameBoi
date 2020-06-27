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

    private ulong prevCpuCycle = 0;
    private ulong cyclesSinceLastDivTick = 0;
    private ulong cyclesSinceLasTimerTick = 0;
    public void Tick(ulong cpuCycle)
    {
        ulong elapsedCycles = cpuCycle - prevCpuCycle;
        cyclesSinceLastDivTick += elapsedCycles;
        cyclesSinceLasTimerTick += elapsedCycles;
        while (cyclesSinceLastDivTick >= divRatio)
        {
            TickDIV();
            cyclesSinceLastDivTick -= divRatio;
        }
        bool enabled = ReadTAC(out int mode);
        uint ratio = ratios[mode];
        while (enabled && cyclesSinceLasTimerTick >= ratio)
        {
            TickTIMA();
            cyclesSinceLasTimerTick -= ratio;
        }
        prevCpuCycle = cpuCycle;
    }

    private bool ReadTAC(out int mode)
    {
        byte tac = Read(TAC_address);
        mode = tac & 3; // get first two bits
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
        if (tima == 0xFF)
        {
            bus.RequestInterrrupt(InterruptType.Timer);
            tima = Read(TMA_address);
        }
        else tima++;
        Write(TIMA_address, tima);
    }
}