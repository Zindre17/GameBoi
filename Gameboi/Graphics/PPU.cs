using GB_Emulator.Gameboi.Memory;
using GB_Emulator.Gameboi.Memory.Specials;
using static GB_Emulator.Statics.ScreenRelatedAddresses;
using static GB_Emulator.Statics.ScreenSizes;

namespace GB_Emulator.Gameboi.Graphics
{
    public class PPU
    {
        private readonly OAM oam;
        private readonly VRAM vram;

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

        public PPU(LCDC lcdc)
        {
            this.lcdc = lcdc;

            oam = new OAM();
            vram = new VRAM(lcdc, scx, scy, wx, wy);
        }

        public (Byte[], Byte[], Byte[]) GetLineLayers(Byte line)
        {
            Byte[] backgroundLine = vram.GetBackgroundLine(line);
            Byte[] spriteLine = new Byte[pixelsPerLine];

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

                Tile tile = vram.GetTile(pattern, true);

                // color 1-3 obp0, color 5 - 7 obp1 (0 is transparent)
                Byte colorOffset = sprite.Palette ? 4 : 0;

                for (Byte x = 0; x < 8; x++)
                {
                    Byte screenX = sprite.ScreenXstart + x;

                    if (screenX >= 160) break;

                    if (sprite.Hidden && backgroundLine[screenX] != 0) continue;

                    Byte spriteX = sprite.Xflip ? 7 - x : x;

                    Byte color = tile.GetColorCode(spriteX, spriteY);

                    if (color == 0) continue;

                    spriteLine[screenX] = colorOffset + color + 1;
                }
            }

            return (backgroundLine, vram.GetWindowLine(line), spriteLine);
        }

        public void SetOamLock(bool on) => oam.SetLock(on);

        public void Connect(Bus bus)
        {
            bus.SetOam(oam);

            bus.SetVram(vram);

            bus.ReplaceMemory(OBP0_address, obp0);
            bus.ReplaceMemory(OBP1_address, obp1);
            bus.ReplaceMemory(BGP_address, bgp);

            bus.ReplaceMemory(SCX_address, scx);
            bus.ReplaceMemory(SCY_address, scy);

            bus.ReplaceMemory(WX_address, wx);
            bus.ReplaceMemory(WY_address, wy);
        }


    }
}