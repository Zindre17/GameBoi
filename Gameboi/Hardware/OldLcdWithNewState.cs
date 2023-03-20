using System;
using Gameboi.Graphics;
using Gameboi.Memory.Io;
using Gameboi.Memory.Specials;
using static Gameboi.Statics.ScreenSizes;
using static Gameboi.Statics.ScreenTimings;

namespace Gameboi.Hardware;

public class OldLcdWithNewState

{
    private readonly OldPpuWithNewState ppu;

    private readonly ColorPalette cbgp = new();
    private readonly ColorPalette cobp = new();

    private readonly SystemState state;

    public OldLcdWithNewState(SystemState state)
    {
        this.state = state;
        ppu = new OldPpuWithNewState(state);

        modeEnters[2] = () =>
        {
            LcdStatus stat = state.LcdStatus;
            if (stat.IsOAMInterruptEnabled)
            {
                var interruptRequests = new InterruptState(state.InterruptFlags);
                state.InterruptFlags = interruptRequests.WithLcdStatusSet();
            }
        };

        modeEnters[3] = () =>
        {
            LoadLine();
        };

        modeEnters[0] = () =>
        {
            LcdStatus stat = state.LcdStatus;
            if (stat.IsHblankInterruptEnabled)
            {
                var interruptRequests = new InterruptState(state.InterruptFlags);
                state.InterruptFlags = interruptRequests.WithLcdStatusSet();
            }
            ppu.AllowBlockTransfer();
        };
        modeExits[0] = () => state.LineY++;

        modeEnters[1] = () =>
        {
            LcdStatus stat = state.LcdStatus;
            var interruptRequests = new InterruptState(state.InterruptFlags);
            if (stat.IsVblankInterruptEnabled)
            {
                state.InterruptFlags = interruptRequests.WithLcdStatusSet();
            }
            state.InterruptFlags = interruptRequests.WithVerticalBlankSet();
        };
        modeExits[1] = () =>
        {
            state.LineY = 0;
        };

        modeTicks[1] = () => state.LineY = (byte)(pixelLines + (cyclesInMode / 456));
    }

    public byte Mode
    {
        get
        {
            LcdStatus stat = state.LcdStatus;
            return stat.Mode;
        }
        set
        {
            LcdStatus stat = state.LcdStatus;
            state.LcdStatus = stat.WithMode(value);
        }
    }

    public void UseColorScreen(bool on)
    {
        ppu.IsColorMode = on;
    }

    private readonly uint[] modeDurations = new uint[4] { hblankClocks, vblankClocks, mode2Clocks, mode3Clocks };
    private readonly Action[] modeEnters = new Action[4];
    private readonly Action[] modeExits = new Action[4];
    private readonly Action[] modeTicks = new Action[4];

    private ulong cyclesInMode = 0;

    public void Tick()
    {
        LcdControl control = state.LcdControl;
        if (control.IsLcdEnabled is false)
        {
            return;
        }

        cyclesInMode += 1;

        LcdStatus status = state.LcdStatus;
        if ((status.CoincidenceFlag || state.LineY == state.LineYCompare)
            && status.IsCoincidenceInterruptEnabled)
        {
            var interruptRequests = new InterruptState(state.InterruptFlags);
            state.InterruptFlags = interruptRequests.WithLcdStatusSet();
        }

        TickMode();
    }

    private byte TickMode()
    {
        var mode = Mode;
        if (cyclesInMode is 1)
        {
            modeEnters[mode]?.Invoke();
        }

        uint endCycles = modeDurations[mode];
        if (cyclesInMode >= endCycles)
        {
            cyclesInMode -= endCycles;
            modeExits[mode]?.Invoke();
            SetNextMode();
            return (byte)cyclesInMode;
        }
        else
        {
            modeTicks[mode]?.Invoke();
            return 0;
        }
    }

    private void SetNextMode()
    {
        LcdControl control = state.LcdControl;
        if (!control.IsLcdEnabled)
        {
            return;
        }


        if (Mode is 0 && state.LineY != pixelLines)
        {
            Mode = 2;
        }
        else
        {
            Mode = (byte)((Mode + 1) % 4);
        }
    }

    private void LoadLine()
    {
        var (backgroundLine, windowLine, spriteLine) = ppu.GetLineLayers(state.LineY);
        var pixels = new Rgba[pixelsPerLine];
        IPallete pallet = ppu.IsColorMode ? cbgp : new Palette(state.BackgroundPalette);

        for (int i = 0; i < pixelsPerLine; i++)
        {
            byte colorCode = backgroundLine[i];
            pixels[i] = new(pallet.GetColor(colorCode));
        }

        for (int i = 0; i < pixelsPerLine; i++)
        {
            byte colorCode = windowLine[i];
            if (colorCode is 0)
            {
                continue;
            }
            // For now window pixels are in range 1-4 instead of 0-3
            pixels[i] = new(pallet.GetColor((byte)(colorCode - 1)));
        }

        if (ppu.IsColorMode)
        {
            for (int i = 0; i < pixelsPerLine; i++)
            {
                byte colorCode = spriteLine[i];
                if (colorCode is 0)
                {
                    continue;
                }
                pixels[i] = new(cobp.GetColor(colorCode));
            }
        }
        else
        {
            var pallets = new[] { new Palette(state.ObjectPalette0), new Palette(state.ObjectPalette1) };
            for (int i = 0; i < pixelsPerLine; i++)
            {
                byte colorCode = spriteLine[i];
                if (colorCode is 0)
                {
                    continue;
                }
                // 4 colors per pallete
                pixels[i] = new(pallets[colorCode / 4].GetColor((byte)(colorCode % 4)));
            }
        }

        OnLineLoaded?.Invoke(state.LineY, pixels);
    }

    public Action<byte, Rgba[]>? OnLineLoaded;
}

