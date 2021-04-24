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


        private readonly LCDC lcdc;

        public PPU(LCDC lcdc)
        {
            this.lcdc = lcdc;

            oam = new OAM();
            vram = new VRAM(lcdc, scx, scy, wx, wy);
        }

        public Byte[] GetLine(Byte line)
        {

            Byte[] pixelLine = vram.GetBackgroundAndWindowLine(line);

            var sprites = oam.GetSpritesOnLine(line, lcdc.IsDoubleSpriteSize);
            Byte[] spriteLayer = new Byte[pixelsPerLine];

            foreach (var sprite in sprites)
            {
                Byte spriteY = line - sprite.ScreenYstart;
                spriteY = sprite.Yflip ? 7 - spriteY : spriteY;

                Byte pattern = sprite.Pattern;
                if (lcdc.IsDoubleSpriteSize)
                {
                    if (spriteY >= 8)
                    {
                        pattern = sprite.Pattern | 0x01;
                        spriteY -= 8;
                    }
                    else
                        pattern = sprite.Pattern & 0xFE;
                }

                Tile tile = vram.GetTile(pattern, true);

                Palette palette = sprite.Palette ? obp1 : obp0;

                for (Byte x = 0; x < 8; x++)
                {
                    Byte screenX = sprite.ScreenXstart + x;
                    if (screenX >= 160) continue;

                    Byte spriteX = sprite.Xflip ? 7 - x : x;
                    Byte color = tile.GetColorCode(spriteX, spriteY);

                    if (color == 0) continue;

                    if (!sprite.Hidden || pixelLine[screenX] == 0)
                        spriteLayer[screenX] = palette.DecodeColorNumber(color) + 1;
                }
            }

            // merge layers and decode background/window colors
            for (int i = 0; i < pixelsPerLine; i++)
            {
                Byte s = spriteLayer[i];
                if (s == 0)
                    pixelLine[i] = bgp.DecodeColorNumber(pixelLine[i]);
                else
                    pixelLine[i] = s - 1;
            }
            return pixelLine;
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