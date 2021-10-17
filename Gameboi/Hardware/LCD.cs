using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GB_Emulator.Gameboi.Graphics;
using GB_Emulator.Gameboi.Memory;
using GB_Emulator.Gameboi.Memory.Specials;
using static GB_Emulator.Statics.ScreenRelatedAddresses;
using static GB_Emulator.Statics.ScreenSizes;
using static GB_Emulator.Statics.ScreenTimings;
using Byte = GB_Emulator.Gameboi.Memory.Byte;

namespace GB_Emulator.Gameboi.Hardware
{
    public class LCD : Hardware, IUpdateable

    {
        private readonly PPU ppu;

        private WriteableBitmap screen;
        public ImageSource Screen => screen;

        private readonly STAT stat = new();
        private readonly LCDC lcdc;

        private readonly LY ly;
        private readonly Register lyc = new();

        private readonly ColorPalette cbgp = new();
        private readonly ColorPalette cobp = new();

        public LCD()
        {
            lcdc = new LCDC(OnScreenToggled);
            ly = new LY(CheckCoincidence);
            ppu = new PPU(lcdc);

            modeEnters[2] = () =>
            {
                ppu.SetOamLock(true);
                ppu.SetVramLock(false);
                if (stat.IsOAMInterruptEnabled)
                    bus.RequestInterrupt(InterruptType.LCDC);
            };

            modeEnters[3] = () =>
            {
                ppu.SetOamLock(true);
                ppu.SetVramLock(true);
                LoadLine();
            };

            modeEnters[0] = () =>
            {
                ppu.SetOamLock(false);
                ppu.SetVramLock(false);
                if (stat.IsHblankInterruptEnabled)
                    bus.RequestInterrupt(InterruptType.LCDC);
                ppu.AllowBlockTransfer();
            };
            modeExits[0] = ly.Increment;

            modeEnters[1] = () =>
            {
                ppu.SetOamLock(false);
                ppu.SetVramLock(false);
                if (stat.IsVblankInterruptEnabled)
                    bus.RequestInterrupt(InterruptType.LCDC);
                bus.RequestInterrupt(InterruptType.VBlank);
            };
            modeExits[1] = () =>
            {
                ly.Reset();
            };

            modeTicks[1] = () => ly.Set(pixelLines + (cyclesInMode / 456));
        }

        public void UseColorScreen(bool on)
        {
            ppu.IsColorMode = on;
            screen = new WriteableBitmap(
                pixelsPerLine,
                pixelLines,
                1,
                1,
                on ? PixelFormats.Rgb24 : PixelFormats.Gray2,
                null
            );
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

        private readonly uint[] modeDurations = new uint[4] { hblankClocks, vblankClocks, mode2Clocks, mode3Clocks };
        private readonly Action[] modeEnters = new Action[4];
        private readonly Action[] modeExits = new Action[4];
        private readonly Action[] modeTicks = new Action[4];

        private void CheckCoincidence(Byte newLY)
        {
            stat.CoincidenceFlag = newLY == lyc.Read();
            if (stat.IsCoincidenceInterruptEnabled && stat.CoincidenceFlag)
                bus.RequestInterrupt(InterruptType.LCDC);
        }

        public override void Connect(Bus bus)
        {
            base.Connect(bus);

            ppu.Connect(bus);

            bus.ReplaceMemory(LCDC_address, lcdc);
            bus.ReplaceMemory(STAT_address, stat);

            bus.ReplaceMemory(LY_address, ly);
            bus.ReplaceMemory(LYC_address, lyc);

            bus.RouteMemory(CBGP_address, cbgp);
            bus.RouteMemory(COBP_address, cobp);
        }

        private ulong cyclesInMode = 0;

        public void Update(byte cycles, ulong speed)
        {
            var cyclesToAdd = cycles / speed;
            cyclesInMode += cyclesToAdd;

            var cyclesLeft = cyclesToAdd;
            while (cyclesLeft != 0)
            {
                cyclesLeft = ExecuteMode(cyclesLeft);
            }
        }

        private byte ExecuteMode(ulong cycles)
        {
            var mode = stat.Mode;
            if (cyclesInMode == cycles)
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
            if (!lcdc.IsEnabled) return;

            byte mode = stat.Mode;
            if (mode == 0 && ly.Y != pixelLines)
            {
                stat.Mode = 2;
            }
            else
            {
                stat.Mode = (mode + 1) % 4;
            }
        }

        private bool showBackground = true;
        private bool showWindow = true;
        private bool showSprites = true;
        public void ToggleBackground()
        {
            showBackground = !showBackground;
        }
        public void ToggleWindow()
        {
            showWindow = !showWindow;
        }
        public void ToggleSprites()
        {
            showSprites = !showSprites;
        }

        private void LoadLine()
        {
            int firstPixelIndex = ly.Y * pixelsPerLine;
            (Byte[] b, Byte[] w, Byte[] s) = ppu.GetLineLayers(ly.Y);
            for (int i = 0; i < pixelsPerLine; i++)
            {
                backgroundLayer[firstPixelIndex + i] = showBackground ? b[i] : 0;
                windowLayer[firstPixelIndex + i] = showWindow ? w[i] : 0;
                spriteLayer[firstPixelIndex + i] = showSprites ? s[i] : 0;
            }
        }



        private readonly Byte[] backgroundLayer = new Byte[pixelsPerLine * pixelLines];
        private readonly Byte[] windowLayer = new Byte[pixelsPerLine * pixelLines];
        private readonly Byte[] spriteLayer = new Byte[pixelsPerLine * pixelLines];


        private byte[] PreparePixels()
        {
            byte[] pixels = new byte[backgroundLayer.Length * screen.Format.BitsPerPixel / 8];
            //grayscale
            if (screen.Format.BitsPerPixel == 2)
            {
                Palette[] sps = new Palette[2] { ppu.Obp0, ppu.Obp1 };
                byte colorsPerPalette = 4;
                for (int i = 0; i < backgroundLayer.Length; i++)
                {
                    var position = (3 - i % 4) * 2;

                    Byte pixel = spriteLayer[i];
                    if (pixel != 0)
                    {
                        pixels[i / 4] |= (byte)(sps[pixel / 4].DecodeColorNumber((byte)(pixel % colorsPerPalette)) << position);
                        continue;
                    }

                    pixel = windowLayer[i];
                    if (pixel-- == 0) // Shift window color back or use background instead
                        pixel = backgroundLayer[i];

                    pixels[i / 4] |= (byte)(ppu.Bgp.DecodeColorNumber(pixel) << position);
                }
            }
            //color
            else
            {
                for (int i = 0; i < backgroundLayer.Length; i++)
                {
                    int start = i * 3;

                    Byte pixel = spriteLayer[i];
                    if (pixel != 0)
                    {
                        var (sr, sg, sb) = cobp.DecodeColorNumber(pixel);
                        pixels[start] = sr;
                        pixels[start + 1] = sg;
                        pixels[start + 2] = sb;
                        continue;
                    }

                    pixel = windowLayer[i];
                    if (pixel-- == 0) // Shift window color back or use background instead
                        pixel = backgroundLayer[i];

                    var (r, g, b) = cbgp.DecodeColorNumber(pixel);
                    pixels[start] = r;
                    pixels[start + 1] = g;
                    pixels[start + 2] = b;
                }
            }
            return pixels;
        }

        private static readonly Int32Rect rect = new(0, 0, pixelsPerLine, pixelLines);

        public void DrawFrame()
        {
            if (screen is not null)
            {
                byte[] pixels = PreparePixels();
                if (lcdc.IsEnabled)
                {
                    screen.WritePixels(rect, pixels, pixels.Length / rect.Height, 0);
                    return;
                }
                screen.WritePixels(rect, new byte[pixels.Length], pixels.Length / rect.Height, 0);
            }
        }
    }
}