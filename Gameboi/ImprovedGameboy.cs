using System;
using Gameboi.Cartridges;
using Gameboi.Graphics;
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
    private readonly Joypad joypad;

    // cpu and bus
    private readonly OldCpuWithNewState cpu;

    public ImprovedGameboy(
        SystemState state,
        IMemoryBankControllerLogic mbc
        )
    {
        this.state = state;
        var bus = new ImprovedBus(state, mbc);

        lcd = new ImprovedLcd(state);
        lcd.OnLineReady += (line, data) =>
        {
            OnPixelRowReady?.Invoke(line, data);
        };
        timer = new ImprovedTimer(state);
        dma = new Dma(state, bus);
        joypad = new Joypad(state);
        cpu = new OldCpuWithNewState(state, bus);
    }

    private const int TicksPerFrame = 70224;

    public Joypad Joypad => joypad;

    public Action<byte, Rgba[]>? OnPixelRowReady;

    public void PlayFrame()
    {
        state.TicksElapsedThisFrame = 0;
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
