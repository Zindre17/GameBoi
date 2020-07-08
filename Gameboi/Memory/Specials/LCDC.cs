using static ByteOperations;

class LCDC : Register
{
    public LCDC() : base(0x91) { }

    public bool IsEnabled { get => data[7]; set => Write(value ? SetBit(7, data) : ResetBit(7, data)); }
    public bool WdMapSelect { get => data[6]; set => Write(value ? SetBit(6, data) : ResetBit(6, data)); }
    public bool IsWindowEnabled { get => data[5]; set => Write(value ? SetBit(5, data) : ResetBit(5, data)); }
    public bool BgWdDataSelect { get => data[4]; set => Write(value ? SetBit(4, data) : ResetBit(4, data)); }

    public bool BgMapSelect { get => data[3]; set => Write(value ? SetBit(3, data) : ResetBit(3, data)); }
    public bool IsDoubleSpriteSize { get => data[2]; set => Write(value ? SetBit(2, data) : ResetBit(2, data)); }
    public bool IsSpritesEnabled { get => data[1]; set => Write(value ? SetBit(1, data) : ResetBit(1, data)); }
    public bool IsBackgroundEnabled { get => data[0]; set => Write(value ? SetBit(0, data) : ResetBit(0, data)); }

}