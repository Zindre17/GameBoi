using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static ByteOperations;

class LCD : Hardware<MainBus>

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


    private byte STAT;
    private bool IsCoincidenceInterruptEnabled { get => TestBit(6, LCDC); set => LCDC = value ? SetBit(6, LCDC) : ResetBit(6, LCDC); }
    private bool IsOAMInterruptEnabled { get => TestBit(5, LCDC); set => LCDC = value ? SetBit(5, LCDC) : ResetBit(5, LCDC); }
    private bool IsVblankInterruptEnabled { get => TestBit(4, LCDC); set => LCDC = value ? SetBit(4, LCDC) : ResetBit(4, LCDC); }
    private bool IsHblankInterruptEnabled { get => TestBit(3, LCDC); set => LCDC = value ? SetBit(3, LCDC) : ResetBit(3, LCDC); }
    private bool CoincidenceFlag { get => TestBit(2, LCDC); set => LCDC = value ? SetBit(2, LCDC) : ResetBit(2, LCDC); }
    private byte prevMode;
    private byte mode;
    private void SetMode(byte newMode)
    {
        STAT &= (0xFC); // reset 2 lowest bits
        STAT |= (byte)(newMode & 3); //copy 2 lowest  bits from mode
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
                    LoadTiles();

                if (WillBeDone(mode3End))
                {
                    SetMode(0);
                    UpdateElapsed(mode3End);
                    DrawLine();
                    // isFrameDone = true;
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

                if (prevMode != mode && IsVblankInterruptEnabled)
                    bus.RequestInterrrupt(InterruptType.VBlank);

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


    private void LoadTiles()
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


    private void DrawLine()
    {
        //draw background
        byte scrollY = Read(SCY_address);
        int tileRow = ((LY + scrollY) / 8) % tileRows;

        int tileMapRowOffset = tileRow * tilesPerRow;
        byte scrollX = Read(SCX_address);
        int tileMapColumnOffset = scrollX / 8;

        byte tileX = (byte)(scrollX % 8);
        byte tileY = (byte)((scrollY + LY) % 8);

        byte[] colorRow = new byte[screenWidth];
        byte tilePattern = backgroundMap[tileMapRowOffset + tileMapColumnOffset];
        Tile tile = LoadTilePattern(tilePattern);
        Palette bgAndWPalette = new Palette(Read(BGP_address));

        for (int pixel = 0; pixel < screenWidth; pixel++)
        {
            if (tileX == 8)
            {
                tileX = 0;
                tileMapColumnOffset++;
                tileMapColumnOffset %= tilesPerRow;
                tilePattern = backgroundMap[tileMapColumnOffset + tileMapRowOffset];
                tile = LoadTilePattern(tilePattern);
            }
            byte color = tile.GetPaletteColor(tileX, tileY);
            colorRow[pixel] = bgAndWPalette.DecodeColorNumber(color);
            tileX++;
        }
        //TODO:draw window

        //TODO:draw sprites
        Int32Rect rect = new Int32Rect(0, LY, screenWidth, 1);

        DrawLineToScreen(rect, colorRow);
    }

    private void DrawLineToScreen(Int32Rect rect, byte[] colors)
    {
        byte[] pixels = new byte[colors.Length / 4];
        int pixelIndex = 0;
        byte pixelsBatch = 0;
        for (int i = 0; i < colors.Length; i++)
        {
            byte value = (byte)((0x03 & colors[i]) << ((3 - (i % 4))) * 2);
            pixelsBatch |= value;
            if (i % 4 == 3)
            {
                pixels[pixelIndex++] = pixelsBatch;
                pixelsBatch = 0;
            }
        }
        screen.WritePixels(rect, pixels, colors.Length / 4 / rect.Height, 0);
    }

    private Tile LoadTilePattern(byte patternIndex)
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