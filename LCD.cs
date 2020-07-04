using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static ByteOperations;

class LCD : Hardware

{
    private const ushort LCDC_address = 0xFF40;
    private const ushort STAT_address = 0xFF41;
    private const ushort SCY_address = 0xFF42;
    private const ushort SCX_address = 0xFF43;
    private const ushort LY_address = 0xFF44;
    private const ushort LYC_address = 0xFF45;
    private const ushort DMA_address = 0xFF46;
    private const ushort BGP_address = 0xFF47;
    private const ushort OBP0_address = 0xFF48;
    private const ushort OBP1_address = 0xFF49;
    private const ushort WY_address = 0xFF4A;
    private const ushort WX_address = 0xFF4B;


    private const ushort TileData1 = 0x8000;
    private const ushort TileData0 = 0x8800;
    private const ushort TileMap0Address = 0x9800;
    private const ushort TileMap1Address = 0x9C00;

    private const byte screenWidth = 160;
    private const byte screenHeight = 144;

    private WriteableBitmap screen;

    public WriteableBitmap Screen => screen;


    private Byte STAT;
    private bool IsCoincidenceInterruptEnabled { get => TestBit(6, STAT); set => STAT = value ? SetBit(6, STAT) : ResetBit(6, STAT); }
    private bool IsOAMInterruptEnabled { get => TestBit(5, STAT); set => STAT = value ? SetBit(5, STAT) : ResetBit(5, STAT); }
    private bool IsVblankInterruptEnabled { get => TestBit(4, STAT); set => STAT = value ? SetBit(4, STAT) : ResetBit(4, STAT); }
    private bool IsHblankInterruptEnabled { get => TestBit(3, STAT); set => STAT = value ? SetBit(3, STAT) : ResetBit(3, STAT); }
    private bool CoincidenceFlag { get => TestBit(2, STAT); set => STAT = value ? SetBit(2, STAT) : ResetBit(2, STAT); }
    private byte prevMode;
    private byte mode;
    private void SetMode(byte newMode)
    {
        STAT &= 0xFC; // reset 2 lowest bits
        STAT |= newMode & 3; //copy 2 lowest  bits from mode
        prevMode = mode;
        mode = newMode;
    }


    private byte LCDC;
    private bool IsEnabled { get => TestBit(7, LCDC); set => LCDC = value ? SetBit(7, LCDC) : ResetBit(7, LCDC); }
    private bool IsWindowTileMap1 { get => TestBit(6, LCDC); set => LCDC = value ? SetBit(6, LCDC) : ResetBit(6, LCDC); }
    private bool IsWindowEnabled { get => TestBit(5, LCDC); set => LCDC = value ? SetBit(5, LCDC) : ResetBit(5, LCDC); }
    private bool IsBgAndWTileData1 { get => TestBit(4, LCDC); set => LCDC = value ? SetBit(4, LCDC) : ResetBit(4, LCDC); }
    private bool IsBgTileMap1 { get => TestBit(3, LCDC); set => LCDC = value ? SetBit(3, LCDC) : ResetBit(3, LCDC); }
    private bool IsDoubleSpriteSize { get => TestBit(2, LCDC); set => LCDC = value ? SetBit(2, LCDC) : ResetBit(2, LCDC); }
    private bool IsSpritesEnabled { get => TestBit(1, LCDC); set => LCDC = value ? SetBit(1, LCDC) : ResetBit(1, LCDC); }
    private bool IsBackgroundEnabled { get => TestBit(0, LCDC); set => LCDC = value ? SetBit(0, LCDC) : ResetBit(0, LCDC); }


    private byte LY;
    private byte LYC;

    private ulong lastCpuCycle = 0;
    private ulong cycle = 0;
    private const uint clocksPerDraw = 70224;
    private const ushort vblankClocks = 4560;

    private byte pixelsDrawnThisLine = 0;
    private const byte mode2End = 80;
    private const byte mode3End = 172 + mode2End;
    private const ushort hblankEnd = 204 + mode3End;


    private byte[] loadedOamData;

    private void Read()
    {
        LCDC = Read(LCDC_address);
        STAT = Read(STAT_address);
        prevMode = mode;
        mode = (byte)(STAT & 3);
        LY = Read(LY_address);
        LYC = Read(LYC_address);
    }
    private void Write()
    {
        Write(LCDC_address, LCDC);
        Write(STAT_address, STAT);
        Write(LY_address, LY);
        Write(LYC_address, LYC);
    }

    private ulong currentFrame = 0;
    private ulong lastMapLoad = 1;
    private ulong lastSpriteLoad = 1;
    public bool Tick(ulong cpuCycle)
    {
        Read();

        bool isFrameDone = false;
        ulong elapsed = cpuCycle - lastCpuCycle;

        void UpdateElapsed(ushort stepEnd, bool reset = false)
        {
            elapsed = cycle + elapsed - stepEnd;
            if (reset)
                cycle = 0;
            else
                cycle = stepEnd;
        }

        void SpendTime()
        {
            cycle += elapsed;
            elapsed = 0;
        }

        bool WillBeDone(ushort stepEnd) => (cycle + elapsed) >= stepEnd;

        while (elapsed != 0)
        {
            // search OAM
            if (mode == 2)
            {
                // since the cpu cant edit OAM when LCD is accessing it,
                // just read all sprite attributes on first tick in this mode
                if (prevMode != mode)
                {
                    if (IsOAMInterruptEnabled)
                        bus.RequestInterrrupt(InterruptType.LCDC);

                    if (lastSpriteLoad != currentFrame)
                        LoadSprites();
                }
                if (WillBeDone(mode2End))
                {
                    SetMode(3);
                    UpdateElapsed(mode2End);
                }
                else
                {
                    SpendTime();
                }
            }
            // Transfer data to LCD driver
            if (mode == 3)
            {
                if (prevMode != mode && lastMapLoad != currentFrame)
                    LoadTileMaps();

                if (WillBeDone(mode3End))
                {
                    SetMode(0);
                    UpdateElapsed(mode3End);
                    PrepareLine();
                }
                else
                {
                    SpendTime();
                }
            }
            // H-Blank
            if (mode == 0)
            {
                if (prevMode != mode && IsHblankInterruptEnabled)
                    bus.RequestInterrrupt(InterruptType.LCDC);

                if (WillBeDone(hblankEnd))
                {
                    //Set mode to 2 or 1 depending on ++LY
                    SetLineY(++LY);
                    if (LY == screenHeight)
                    {
                        SetMode(1);
                        UpdateElapsed(hblankEnd, true);
                    }
                    else
                    {
                        SetMode(2);
                        UpdateElapsed(hblankEnd, true);
                    }
                }
                else
                {
                    SpendTime();
                }
            }
            // V-Blank
            if (mode == 1)
            {

                if (prevMode != mode)
                {
                    DrawFrame();
                    if (IsVblankInterruptEnabled)
                        bus.RequestInterrrupt(InterruptType.VBlank);
                }

                if (WillBeDone(vblankClocks))
                {
                    SetLineY(0);
                    UpdateElapsed(vblankClocks, true);
                    isFrameDone = true;
                    currentFrame++;
                    SetMode(2);
                }
                else
                {
                    double progress = elapsed / vblankClocks;
                    SetLineY((byte)(LY + (progress * scanlinesOffScreen)));
                    SpendTime();
                }
            }
        }
        lastCpuCycle = cpuCycle;
        Write();
        return isFrameDone;
    }
    private const byte maxLY = 153;
    private const byte scanlinesOffScreen = maxLY - screenHeight;
    private Sprite[] spriteAttributes = new Sprite[40];
    private void LoadSprites()
    {
        lastSpriteLoad = currentFrame;
        ushort address = 0xFE00;
        int index = 0;
        while (address < 0xFEA0)
        {
            Sprite sprite = new Sprite(Read(address++), Read(address++), Read(address++), Read(address++));
            spriteAttributes[index] = sprite;
            index++;
        }
    }

    private const ushort mapSize = tilesPerRow * tileRows;
    private byte[] backgroundMap = new byte[mapSize];
    private byte[] windowMap = new byte[20 * 18];


    private void LoadTileMaps()
    {
        lastMapLoad = currentFrame;
        ushort bgAddress = IsBgTileMap1 ? TileMap1Address : TileMap0Address;

        ushort wAddress = IsWindowTileMap1 ? TileMap1Address : TileMap0Address;

        // load bg and w tilemaps
        for (ushort i = 0; i < mapSize; i++)
        {
            byte tile = Read((ushort)(bgAddress + i));
            backgroundMap[i] = tile;

            //might need to change this to skip tiles 20 - 32 per row
            if (i < windowMap.Length)
            {
                tile = Read((ushort)(wAddress + i));
                windowMap[i] = tile;
            }
        }
    }

    private const byte tilesPerRow = 32;
    private const byte tileRows = 32;
    private const byte tilesPerScreenRow = 20;

    private byte[] pixels = new byte[screenWidth * screenHeight];

    private Tile GetBackgroundTileAtPoint(Byte x, Byte y)
    {
        if (!IsBackgroundEnabled) return new Tile();
        return GetTileAtPoint(x, y, backgroundMap, tilesPerRow);
    }
    private Tile GetTileAtPoint(Byte x, Byte y, byte[] map, Byte width)
    {
        Address index = (y / 8) * width + (x / 8);
        return LoadTilePattern(map[index]);
    }
    private Tile GetWindowTile(Byte x, Byte y)
    {
        if (!IsWindowEnabled) return new Tile();
        return GetTileAtPoint(x, y, windowMap, 20);
    }


    private void PrepareLine()
    {
        int firstPixelIndex = LY * screenWidth;

        Palette palette = new Palette(Read(BGP_address));

        Byte y = Read(SCY_address) + LY;
        Byte x = Read(SCX_address);

        Byte tileY = y % 8;
        Tile tile = GetBackgroundTileAtPoint(x, y);
        for (int i = 0; i < screenWidth; i++)
        {
            Byte tileX = x % 8;
            Byte colorCode = tile.GetColorCode(tileX, tileY);
            pixels[i + firstPixelIndex] = palette.DecodeColorNumber(colorCode);
            x++;
            if (tileX == 7)
            {
                tile = GetBackgroundTileAtPoint(x, y);
            }
        }

        //window
        if (IsWindowEnabled)
        {
            Byte yWindow = LY - Read(WY_address);
            tileY = yWindow % 8;

            Byte xWindow = Read(WX_address);
            int sx = xWindow.Value - 7;

            //ignore space outside
            if (sx < 0)
            {
                int equalizer = 0 - sx;
                xWindow += equalizer;
                x = equalizer;
            }
            tile = GetWindowTile(xWindow, yWindow);
            for (int i = x; i < screenWidth; i++)
            {
                Byte tileX = xWindow % 8;
                Byte colorCode = tile.GetColorCode(tileX, tileY);
                pixels[i + firstPixelIndex] = palette.DecodeColorNumber(colorCode);
                xWindow++;
                if (tileX == 7)
                {
                    tile = GetWindowTile(xWindow, yWindow);
                }
            }
        }

        //sprites
        if (IsSpritesEnabled)
        {
            int spriteHeight = IsDoubleSpriteSize ? 16 : 8;
            foreach (Sprite s in spriteAttributes)
            {
                int screenY = s.Y - 16;
                if (screenY <= LY && LY < screenY + spriteHeight) // LY is somewhere within the sprite
                {
                    palette = new Palette(Read(s.Palette ? OBP1_address : OBP0_address));
                    tile = LoadTilePattern(s.Pattern); // swap with correct tileDataTable lookup
                    int tx;
                    int ti;
                    if (s.Xflip)
                    {
                        tx = 7;
                        ti = -1;
                    }
                    else
                    {
                        tx = 0;
                        ti = 1;
                    }
                    int ty = LY - screenY;
                    if (s.Yflip) ty = spriteHeight - ty - 1;
                    for (int screenX = s.X - 8; screenX < screenX + 8; screenX++)
                    {
                        if (screenX >= 0 && screenX < screenWidth)
                        {
                            Byte colorCode = tile.GetColorCode(tx, ty);
                            pixels[screenX] = palette.DecodeColorNumber(colorCode);
                        }
                        tx += ti;
                    }
                }
            }
        }
    }

    private static readonly Int32Rect rect = new Int32Rect(0, 0, screenWidth, screenHeight);
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

    private Tile LoadTilePattern(Byte patternIndex)
    {

        bool isSignedIndex = !IsBgAndWTileData1;
        ushort startAddress = IsBgAndWTileData1 ? TileData1 : TileData0;
        ushort offset = isSignedIndex ? (ushort)(((sbyte)patternIndex - sbyte.MinValue) * 16) : (ushort)(patternIndex * 16);
        byte[] data = new byte[16];
        ushort address = (ushort)(startAddress + offset);
        for (int i = 0; i < 16; i++)
        {
            data[i] = Read(address++);
        }
        return new Tile(data);
    }
    private void SetLineY(byte line)
    {
        LY = line;
        //compare LY and LYC 
        CoincidenceFlag = LY == LYC;
        if (IsCoincidenceInterruptEnabled && CoincidenceFlag)
            bus.RequestInterrrupt(InterruptType.LCDC);
    }

    public LCD()
    {
        screen = new WriteableBitmap(
            screenWidth,
            screenHeight,
            1,
            1,
            PixelFormats.Gray2,
            null);
    }


}