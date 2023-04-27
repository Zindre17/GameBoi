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
    private readonly OldVramDmaWithNewState vramDma;
    private readonly Joypad joypad;
    private readonly SerialTransfer serial;

    // cpu and bus
    private readonly OldCpuWithNewState cpu;

    public ImprovedGameboy(
        SystemState state,
        IMemoryBankControllerLogic mbc
        )
    {
        this.state = state;
        var bus = new ImprovedBus(state, mbc);

        cpu = new OldCpuWithNewState(state, bus);
        cpu.Init();

        dma = new Dma(state, bus);
        vramDma = new OldVramDmaWithNewState(state, bus);

        lcd = new ImprovedLcd(state, vramDma);
        lcd.OnLineReady += (line, data) =>
        {
            OnPixelRowReady?.Invoke(line, data);
        };
        timer = new ImprovedTimer(state);
        joypad = new Joypad(state);
        serial = new SerialTransfer(state);
    }

    private const int TicksPerFrame = 70224;

    public Joypad Joypad => joypad;

    public Action<byte, Rgba[]>? OnPixelRowReady;

    public void PlayFrame()
    {
        var ticksElapsedThisFrame = 0;
        while (ticksElapsedThisFrame < TicksPerFrame)
        {
            ticksElapsedThisFrame++;

            joypad.CheckInputs();

            serial.Tick();
            timer.Tick();
            lcd.Tick();
            dma.Tick();
            if (state.IsVramDmaInProgress && !state.VramDmaModeIsHblank && state.TicksLeftOfInstruction is 0)
            {
                vramDma.Tick();
            }
            else
            {
                cpu.Tick();
            }

            if (state.IsInDoubleSpeedMode)
            {
                timer.Tick();
                dma.Tick();
                if (!state.IsVramDmaInProgress)
                {
                    cpu.Tick();
                }
            }
        }
    }
}
