using GB_Emulator.Gameboi.Memory;
using GB_Emulator.Gameboi.Memory.Specials;
using static GB_Emulator.Statics.GeneralMemoryMap;
using static GB_Emulator.Statics.ScreenRelatedAddresses;
using static GB_Emulator.Statics.ScreenSizes;

namespace GB_Emulator.Gameboi.Graphics
{
    public class PPU
    {
        private readonly OAM oam;
        private readonly VRAM vram;
        private readonly VramDma vramDma = new();

        private readonly Palette obp0 = new(0xFF);
        private readonly Palette obp1 = new(0xFF);
        private readonly Palette bgp = new(0xFC);

        private readonly Register scy = new();
        private readonly Register scx = new();

        private readonly Register wx = new();
        private readonly Register wy = new();

        public Palette Obp0 => obp0;
        public Palette Obp1 => obp1;
        public Palette Bgp => bgp;

        private readonly LCDC lcdc;

        private bool isColorMode;
        public bool IsColorMode
        {
            get => isColorMode;
            set
            {
                vram.SetColorMode(value);
                isColorMode = value;
            }
        }

        public PPU(LCDC lcdc)
        {
            this.lcdc = lcdc;

            oam = new OAM();
            vram = new VRAM(lcdc, scx, scy, wx, wy);
        }

        public (Byte[], Byte[], Byte[]) GetLineLayers(Byte line)
        {
            var backgroundLine = vram.GetBackgroundLine(line);
            var windowLine = vram.GetWindowLine(line);
            var spriteLine = GetSpriteLine(line, lcdc, backgroundLine);

            return (backgroundLine, windowLine, spriteLine);
        }

        public Byte[] GetSpriteLine(Byte line, LCDC lcdc, Byte[] backgroundLine)
        {
            var spriteLine = new Byte[pixelsPerLine];

            if (!lcdc.IsSpritesEnabled)
                return spriteLine;

            var sprites = oam.GetSpritesOnLine(line, lcdc.IsDoubleSpriteSize);

            foreach (var sprite in sprites)
            {
                Byte spriteY = line - sprite.ScreenYstart;

                Byte pattern = sprite.Pattern;

                if (lcdc.IsDoubleSpriteSize)
                {
                    spriteY = sprite.Yflip ? 15 - spriteY : spriteY;
                    if (spriteY >= 8)
                    {
                        pattern = sprite.Pattern | 0x01;
                        spriteY -= 8;
                    }
                    else
                        pattern = sprite.Pattern & 0xFE;
                }
                else
                {
                    spriteY = sprite.Yflip ? 7 - spriteY : spriteY;
                }

                Tile tile = vram.GetTile(pattern, IsColorMode ? sprite.VramBank : 0, true);

                // color 1-3 obp0, color 5 - 7 obp1 (0 is transparent)
                Byte colorOffset = IsColorMode ? sprite.ColorPalette * 4 : sprite.Palette ? 4 : 0;

                for (Byte x = 0; x < 8; x++)
                {
                    var screenXint = sprite.ScreenXstart + x;
                    if (screenXint >= 160) break; // outside on the right already. No need to check the rest of the sprite
                    if (screenXint < 0) continue; // outside on the left, but should still check the rest of the sprite

                    Byte screenX = screenXint;

                    if (sprite.Hidden && backgroundLine[screenX] % 4 > 0) continue; // background has priority

                    Byte spriteX = sprite.Xflip ? 7 - x : x;

                    Byte color = tile.GetColorCode(spriteX, spriteY);
                    if (color == 0) continue; // transparent

                    spriteLine[screenX] = colorOffset + color;
                }
            }
            return spriteLine;
        }

        public void SetOamLock(bool on) => oam.SetLock(on);
        public void AllowBlockTransfer() => vramDma.TransferIfActive();

        public void Connect(Bus bus)
        {
            vramDma.Connect(bus);
            bus.SetOam(oam);

            bus.SetVram(vram);

            bus.ReplaceMemory(OBP0_address, obp0);
            bus.ReplaceMemory(OBP1_address, obp1);
            bus.ReplaceMemory(BGP_address, bgp);

            bus.ReplaceMemory(SCX_address, scx);
            bus.ReplaceMemory(SCY_address, scy);

            bus.ReplaceMemory(WX_address, wx);
            bus.ReplaceMemory(WY_address, wy);
            bus.ReplaceMemory(VRAM_SwitchAddress, vram.BankSelectRegister);
        }


    }
}