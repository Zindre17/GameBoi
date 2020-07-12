using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static ScreenRelatedAddresses;
using static ScreenSizes;
using static ScreenTimings;
using System;

class LCD : Hardware

{
    private PPU ppu;

    private WriteableBitmap screen;
    public ImageSource Screen => screen;

    private STAT stat = new STAT();
    private LCDC lcdc;

    private LY ly;
    private Register lyc = new Register();

    public LCD()
    {
        lcdc = new LCDC(stat, OnScreenToggled);
        ly = new LY(CheckCoincidence);
        ppu = new PPU(lcdc);

        screen = new WriteableBitmap(
            pixelsPerLine,
            pixelLines,
            1,
            1,
            PixelFormats.Gray2,
            null);

        modeEnters[0] = LoadLine;
        modeEnters[1] = DrawFrame;
        modeEnters[2] = () => ppu.SetOamLock(true);
        modeEnters[3] = () => ppu.SetVramLock(true);

        modeExits[0] = ly.Increment;
        modeExits[1] = () =>
        {
            ly.Reset();
            isFrameDone = true;
            currentFrame++;
        };

        modeTicks[1] = () => ly.Set(pixelLines + (cyclesInMode / 456));
    }

    private void OnScreenToggled(bool on)
    {
        cyclesInMode = 0;
        if (on)
        {
            ly.Reset();
            stat.Mode = 2;
            modeDurations[1] = vblankClocks;
        }
        else
        {
            stat.Mode = 1;
            modeDurations[1] = clocksPerDraw;
            ppu.SetOamLock(false);
            ppu.SetVramLock(false);
        }
    }

    private uint[] modeDurations = new uint[4] { hblankClocks, vblankClocks, mode2Clocks, mode3Clocks };
    private Func[] modeEnters = new Func[4];
    private Func[] modeExits = new Func[4];
    private Func[] modeTicks = new Func[4];

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
    bool isFrameDone;

    public bool Tick(Byte elapsedCpuCycles)
    {
        isFrameDone = false;

        cyclesInMode += elapsedCpuCycles;

        while (elapsedCpuCycles != 0)
        {
            Byte mode = stat.Mode;
            elapsedCpuCycles = ExecuteMode(modeEnters[mode], modeExits[mode], modeTicks[mode]);
        }

        return isFrameDone;
    }

    private ulong ExecuteMode(Func onEnter = null, Func onExit = null, Func onTick = null)
    {
        Byte mode = stat.Mode;
        if (prevMode != mode)
        {
            if (onEnter != null) onEnter();

            if (mode == 1) bus.RequestInterrrupt(InterruptType.VBlank);
            else if (mode == 2 || mode == 0) bus.RequestInterrrupt(InterruptType.LCDC);
        }

        uint endCycles = modeDurations[mode];
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
            prevMode = mode;
            return 0;
        }
    }

    private void SetNextMode()
    {
        if (!lcdc.IsEnabled) return;

        byte mode = stat.Mode;
        if (mode == 0 && ly.Y != pixelLines) SetMode(2);
        else SetMode((mode + 1) % 4);
    }

    private void SetMode(Byte mode)
    {
        prevMode = stat.Mode;
        stat.Mode = mode;
    }

    private void LoadLine()
    {
        // hblank => open vram and oam
        ppu.SetOamLock(false);
        ppu.SetVramLock(false);

        int firstPixelIndex = ly.Y * pixelsPerLine;
        Byte[] line = ppu.GetLine(ly.Y);
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
