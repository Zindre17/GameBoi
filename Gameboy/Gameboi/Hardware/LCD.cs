using System;
using GB_Emulator.Gameboi.Graphics;
using GB_Emulator.Gameboi.Memory;
using GB_Emulator.Gameboi.Memory.Specials;
using static GB_Emulator.Statics.ScreenRelatedAddresses;
using static GB_Emulator.Statics.ScreenSizes;
using static GB_Emulator.Statics.ScreenTimings;
using Byte = GB_Emulator.Gameboi.Memory.Byte;

namespace GB_Emulator.Gameboi.Hardware
{
    public class LCD : Hardware, IUpdatable

    {
        private readonly PPU ppu;

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
        }

        private void OnScreenToggled(bool on)
        {
            cyclesInMode = 0;
            if (on)
            {
                ly.Reset();
                stat.Mode = 2;
            }
            else
            {
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
            if (!lcdc.IsEnabled) return;

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


        public record struct Rgba(byte R, byte G, byte B, byte A);

        private static readonly Rgba black = new(0, 0, 0, 0xff);
        private static readonly Rgba darkGray = new(85, 85, 85, 0xff);
        private static readonly Rgba lightGray = new(170, 170, 170, 0xff);
        private static readonly Rgba white = new(0xff, 0xff, 0xff, 0xff);

        private static readonly Rgba[] balckWhiteColors = new[] { black, darkGray, lightGray, white };

        public Rgba[] PreparePixels()
        {
            Rgba[] pixels = new Rgba[backgroundLayer.Length];
            //grayscale
            if (!ppu.IsColorMode)
            {
                Palette[] sps = new Palette[2] { ppu.Obp0, ppu.Obp1 };
                byte colorsPerPalette = 4;
                for (int i = 0; i < backgroundLayer.Length; i++)
                {
                    byte pixel = spriteLayer[i];
                    byte color;
                    if (pixel is not 0)
                    {
                        color = sps[pixel / 4].DecodeColorNumber((byte)(pixel % colorsPerPalette));
                        pixels[i] = balckWhiteColors[color];
                        continue;
                    }

                    pixel = windowLayer[i];
                    if (pixel-- is 0) // Shift window color back or use background instead
                        pixel = backgroundLayer[i];

                    color = ppu.Bgp.DecodeColorNumber(pixel);
                    pixels[i] = balckWhiteColors[color];
                }
            }
            //color
            else
            {
                for (int i = 0; i < backgroundLayer.Length; i++)
                {
                    byte pixel = spriteLayer[i];
                    if (pixel is not 0)
                    {
                        var (sr, sg, sb) = cobp.DecodeColorNumber(pixel);
                        pixels[i] = new(sr, sg, sb, 0xff);
                        continue;
                    }

                    pixel = windowLayer[i];
                    if (pixel-- is 0) // Shift window color back or use background instead
                        pixel = backgroundLayer[i];

                    var (r, g, b) = cbgp.DecodeColorNumber(pixel);

                    pixels[i] = new(r, g, b, 0xff);
                }
            }
            return pixels;
        }
    }
}
