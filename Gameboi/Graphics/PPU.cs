using static ScreenRelatedAddresses;
using static ScreenSizes;
using static GeneralMemoryMap;
class PPU
{
    private OAM oam;
    private VRAM vram;

    private Palette obp0 = new Palette(0xFF, 3);
    private Palette obp1 = new Palette(0xFF, 3);
    private Palette bgp = new Palette(0xFC);

    private Register scy = new Register();
    private Register scx = new Register();

    private Register wx = new Register();
    private Register wy = new Register();


    private LCDC lcdc;

    public PPU(LCDC lcdc)
    {
        this.lcdc = lcdc;

        oam = new OAM();
        vram = new VRAM();
    }

    public byte[] GetLine(Byte line)
    {
        byte[] pixelLine = new byte[pixelsPerLine];

        // var spritesOnLine = oam.GetSpritesOnLine(line, false);

        // byte[] windowLine = vram.GetLine(line, scx.Read(), scy.Read(), lcdc.WdMapSelect, lcdc.BgWdDataSelect);

        byte[] backgroundLine = vram.GetLine(line, scx.Read(), scy.Read(), lcdc.BgMapSelect, lcdc.BgWdDataSelect);

        for (int i = 0; i < pixelsPerLine; i++)
        {
            pixelLine[i] = bgp.DecodeColorNumber(backgroundLine[i]);
        }

        return pixelLine;
    }

    public void Connect(Bus bus)
    {
        bus.SetOam(oam);
        bus.RouteMemory(OAM_StartAddress, oam, OAM_EndAddress);

        bus.SetVram(vram);
        bus.RouteMemory(VRAM_StartAddress, vram, VRAM_EndAddress);

        bus.ReplaceMemory(OBP0_address, obp0);
        bus.ReplaceMemory(OBP1_address, obp1);
        bus.ReplaceMemory(BGP_address, bgp);

        bus.ReplaceMemory(SCX_address, scx);
        bus.ReplaceMemory(SCY_address, scy);

        bus.ReplaceMemory(WX_address, wx);
        bus.ReplaceMemory(WY_address, wy);
    }


}