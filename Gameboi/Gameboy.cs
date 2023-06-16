using System;
using Gameboi.Cartridges;
using Gameboi.Controls;
using Gameboi.Graphics;
using Gameboi.Hardware;
using Gameboi.Processor;
using Gameboi.Sound;

namespace Gameboi;

public class Gameboy
{
    // State
    private readonly SystemState state;

    // Io
    private readonly Lcd lcd;
    private readonly Timer timer;
    private readonly Dma dma;
    private readonly VramDma vramDma;
    private readonly Controller joypad;
    private readonly SerialTransfer serial;
    private readonly Spu spu;

    // cpu and bus
    private readonly Cpu cpu;

    public Gameboy(
        SystemState state,
        IMemoryBankControllerLogic mbc
        )
    {
        this.state = state;
        var bus = new Bus(state, mbc);

        cpu = new Cpu(state, bus);
        cpu.Init();

        dma = new Dma(state, bus);
        vramDma = new VramDma(state, bus);

        lcd = new Lcd(state, vramDma);
        lcd.OnLineReady += (line, data) =>
        {
            OnPixelRowReady?.Invoke(line, data);
        };
        timer = new Timer(state);
        joypad = new Controller(state);
        serial = new SerialTransfer(state);

        spu = new Spu(state);
    }

    private const int TicksPerFrame = 70224;

    public Controller Controller => joypad;

    public Action<byte, Rgba[]>? OnPixelRowReady;

    private double speed = 1f;
    public void SetPlaySpeed(double speed)
    {
        this.speed = speed;
    }

    public void PlayFrame()
    {
        var ticksElapsedThisFrame = 0;
        while (ticksElapsedThisFrame < (TicksPerFrame * speed))
        {
            ticksElapsedThisFrame++;

            spu.Tick();

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
        spu.GenerateNextFrameOfSamples();
    }
}
