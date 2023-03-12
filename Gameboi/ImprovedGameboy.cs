using Gameboi.Cartridges;
using Gameboi.Hardware;

namespace Gameboi;

public class ImprovedGameboy
{
    // State
    private readonly SystemState state;

    // Io
    private readonly ImprovedLcd lcd;
    private readonly ImprovedTimer timer;
    private readonly Dma dma;

    // cpu and bus
    private readonly ImprovedCpu cpu;

    public ImprovedGameboy(
        SystemState state,
        IMemoryBankControllerLogic mbc
        )
    {
        this.state = state;
        var bus = new ImprovedBus(state, mbc);

        lcd = new ImprovedLcd(state);
        timer = new ImprovedTimer(state);
        dma = new Dma(state, bus);
        cpu = new ImprovedCpu(state, bus, new InstructionSet(state, bus));
    }

    private const int TicksPerFrame = 70224;

    public void PlayFrame()
    {
        while (state.TicksElapsedThisFrame < TicksPerFrame)
        {
            state.TicksElapsedThisFrame++;

            timer.Tick();

            cpu.Tick();
            lcd.Tick();
            dma.Tick();
        }
    }
}
