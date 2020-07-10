using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static ScreenRelatedAddresses;
using static GeneralMemoryMap;
using static ScreenSizes;
using static ScreenTimings;
using System;

class LCD : Hardware

{
    private PPU ppu;

    private WriteableBitmap screen;
    public ImageSource Screen => screen;

    private STAT stat = new STAT();
    private LCDC lcdc = new LCDC();

    private LY ly;
    private Register lyc = new Register();

    public LCD()
    {
        ly = new LY(CheckCoincidence);
        ppu = new PPU(lcdc);

        screen = new WriteableBitmap(
            pixelsPerLine,
            pixelLines,
            1,
            1,
            PixelFormats.Gray2,
            null);
    }

    private void CheckCoincidence(Byte newLY)
    {
        stat.CoincidenceFlag = newLY == lyc.Read();
        if (stat.IsCoincidenceInterruptEnabled && stat.CoincidenceFlag)
            bus.RequestInterrrupt(InterruptType.LCDC);
    }

    public override void Connect(Bus bus)
    {
        base.Connect(bus);

        ppu.Connect(bus);

        bus.ReplaceMemory(LCDC_address, lcdc);
        bus.ReplaceMemory(STAT_address, stat);

        bus.ReplaceMemory(LY_address, ly);
        bus.ReplaceMemory(LYC_address, lyc);

    }

    private delegate void Func();
    private ulong currentFrame = 0;
    private ulong cyclesInMode = 0;

    private byte prevMode;

    public bool Tick(Byte elapsedCpuCycles)
    {
        bool isFrameDone = false;

        cyclesInMode += elapsedCpuCycles;

        while (elapsedCpuCycles != 0)
        {
            // search OAM
            if (stat.Mode == 2)
            {
                elapsedCpuCycles = ExecuteMode(
                    mode2End,
                    stat.IsOAMInterruptEnabled,
                    () => ppu.SetOamLock(true)
                );
            }
            // Transfer data to LCD driver
            else if (stat.Mode == 3)
            {
                elapsedCpuCycles = ExecuteMode(
                    mode3End,
                    false,
                    () => ppu.SetVramLock(true)
                );
            }
            // H-Blank
            else if (stat.Mode == 0)
            {
                elapsedCpuCycles = ExecuteMode(
                    hblankEnd,
                    stat.IsHblankInterruptEnabled,
                    () =>
                    {
                        ppu.SetOamLock(false);
                        ppu.SetVramLock(false);
                        LoadLine();
                    },
                    ly.Increment
                );
            }
            // V-Blank
            else if (stat.Mode == 1)
            {
                elapsedCpuCycles = ExecuteMode(
                    vblankClocks,
                    stat.IsVblankInterruptEnabled,
                    DrawFrame,
                    () =>
                    {
                        ly.Reset();
                        isFrameDone = true;
                        currentFrame++;
                    },
                    () => { ly.Set(pixelLines + (cyclesInMode / 456)); }
                );
            }
        }

        return isFrameDone;
    }

    private ulong ExecuteMode(ulong endCycles, bool canInterrupt, Func onEnter = null, Func onExit = null, Func onTick = null)
    {
        if (prevMode != stat.Mode)
        {
            if (onEnter != null) onEnter();
            if (canInterrupt)
                bus.RequestInterrrupt(InterruptType.LCDC);
        }

        if (cyclesInMode >= endCycles)
        {
            cyclesInMode -= endCycles;
            if (onExit != null) onExit();
            SetNextMode();
            return cyclesInMode;
        }
        else
        {
            if (onTick != null) onTick();
            prevMode = stat.Mode;
            return 0;
        }
    }

    private void SetNextMode()
    {
        byte mode = stat.Mode;
        SetMode(mode switch
        {
            0 => ly.Y == pixelLines ? 1 : 2,
            1 => 2,
            2 => 3,
            3 => 0,
            _ => throw new Exception("Impossible")
        });
    }

    private void SetMode(Byte mode)
    {
        prevMode = stat.Mode;
        stat.Mode = mode;
    }

    private void LoadLine()
    {
        int firstPixelIndex = ly.Y * pixelsPerLine;
        byte[] line = ppu.GetLine(ly.Y);
        for (int i = 0; i < pixelsPerLine; i++)
        {
            pixels[firstPixelIndex + i] = line[i];
        }
    }

    private byte[] pixels = new byte[pixelsPerLine * pixelLines];

    private static readonly Int32Rect rect = new Int32Rect(0, 0, pixelsPerLine, pixelLines);
    private void DrawFrame()
    {
        byte[] formattedPixels = new byte[pixels.Length / 4];
        int index = 0;
        for (int i = 0; i < pixels.Length; i += 4)
        {
            Byte value = pixels[i] << 6 | pixels[i + 1] << 4 | pixels[i + 2] << 2 | pixels[i + 3];
            formattedPixels[index++] = value;
        }
        screen.WritePixels(rect, formattedPixels, formattedPixels.Length / rect.Height, 0);
    }

    private void DrawDisabledFrame()
    {
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = 3;
        DrawFrame();
    }
}
