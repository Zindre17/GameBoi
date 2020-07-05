using static ByteOperations;

class LCDC : Register
{
    public LCDC() : base(0x91) { }

    public bool IsEnabled { get => TestBit(7, data); set => Write(value ? SetBit(7, data) : ResetBit(7, data)); }
    public bool IsWindowTileMap1 { get => TestBit(6, data); set => Write(value ? SetBit(6, data) : ResetBit(6, data)); }
    public bool IsWindowEnabled { get => TestBit(5, data); set => Write(value ? SetBit(5, data) : ResetBit(5, data)); }
    public bool IsBgAndWTileData1 { get => TestBit(4, data); set => Write(value ? SetBit(4, data) : ResetBit(4, data)); }
    public bool IsBgTileMap1 { get => TestBit(3, data); set => Write(value ? SetBit(3, data) : ResetBit(3, data)); }
    public bool IsDoubleSpriteSize { get => TestBit(2, data); set => Write(value ? SetBit(2, data) : ResetBit(2, data)); }
    public bool IsSpritesEnabled { get => TestBit(1, data); set => Write(value ? SetBit(1, data) : ResetBit(1, data)); }
    public bool IsBackgroundEnabled { get => TestBit(0, data); set => Write(value ? SetBit(0, data) : ResetBit(0, data)); }

}