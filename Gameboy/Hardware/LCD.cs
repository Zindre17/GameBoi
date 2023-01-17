using System;
using GB_Emulator.Graphics;
using GB_Emulator.Memory;
using GB_Emulator.Memory.Specials;
using static GB_Emulator.Statics.ScreenRelatedAddresses;
using static GB_Emulator.Statics.ScreenSizes;
using static GB_Emulator.Statics.ScreenTimings;
using Byte = GB_Emulator.Memory.Byte;

namespace GB_Emulator.Hardware
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

        public byte Mode { get => stat.Mode; set => stat.Mode = value; }
        public LCDC Controller => lcdc;

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

        public void Update(uint cycles, ulong speed)
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

        private void LoadLine()
        {
            var (backgroundLine, windowLine, spriteLine) = ppu.GetLineLayers(ly.Y);
            var pixels = new Rgba[pixelsPerLine];
            IPallete pallet = ppu.IsColorMode ? cbgp : ppu.Bgp;

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
                var pallets = new[] { ppu.Obp0, ppu.Obp1 };
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

            OnLineLoaded?.Invoke(ly.Y, pixels);
        }

        public Action<byte, Rgba[]>? OnLineLoaded;
    }
}
