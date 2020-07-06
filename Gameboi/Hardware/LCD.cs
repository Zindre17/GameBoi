using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static ScreenRelatedAddresses;
using static GeneralMemoryMap;
using static ScreenTimings;
using System;

class LCD : Hardware

{

    private const byte screenWidth = 160;
    private const byte screenHeight = 144;

    private WriteableBitmap screen;
    public ImageSource Screen => screen;

    private STAT stat = new STAT();
    private LCDC lcdc = new LCDC();

    private LY ly;
    private Register lyc = new Register();

    private Register scy = new Register();
    private Register scx = new Register();

    private Register wx = new Register();
    private Register wy = new Register();

    private Palette bgp = new Palette(0xFC);
    private Palette obp0 = new Palette(0xFF, 3);
    private Palette obp1 = new Palette(0xFF, 3);

    public LCD()
    {
        ly = new LY(CheckCoincidence);

        screen = new WriteableBitmap(
            screenWidth,
            screenHeight,
            1,
            1,
            PixelFormats.Gray2,
            null);
    }

    private void CheckCoincidence(Byte newLY)
    {
        stat.CoincidenceFlag = newLY == lyc.Read();
        if (stat.IsCoincidenceInterruptEnabled && stat.CoincidenceFlag)
            bus.RequestInterrrupt(InterruptType.LCDC);
    }

    public override void Connect(Bus bus)
    {
        base.Connect(bus);

        bus.ReplaceMemory(LCDC_address, lcdc);
        bus.ReplaceMemory(STAT_address, stat);

        bus.ReplaceMemory(LY_address, ly);
        bus.ReplaceMemory(LYC_address, lyc);

        bus.ReplaceMemory(SCX_address, scx);
        bus.ReplaceMemory(SCY_address, scy);

        bus.ReplaceMemory(WX_address, wx);
        bus.ReplaceMemory(WY_address, wy);

        bus.ReplaceMemory(BGP_address, bgp);
        bus.ReplaceMemory(OBP0_address, obp0);
        bus.ReplaceMemory(OBP1_address, obp1);

    }



    private delegate void Func();
    private ulong currentFrame = 0;
    private ulong cyclesInMode = 0;



    private byte prevMode;

    public bool Tick(Byte elapsedCpuCycles)
    {
        bool isFrameDone = false;

        cyclesInMode += elapsedCpuCycles;

        while (elapsedCpuCycles != 0)
        {
            // search OAM
            if (stat.Mode == 2)
            {
                elapsedCpuCycles = ExecuteMode(
                    mode2End,
                    stat.IsOAMInterruptEnabled,
                    LoadSprites
                );
            }
            // Transfer data to LCD driver
            else if (stat.Mode == 3)
            {
                elapsedCpuCycles = ExecuteMode(
                    mode3End,
                    false,
                    LoadTileMaps
                );
            }
            // H-Blank
            else if (stat.Mode == 0)
            {
                elapsedCpuCycles = ExecuteMode(
                    hblankEnd,
                    stat.IsHblankInterruptEnabled,
                    PrepareLine,
                    ly.Increment
                );
            }
            // V-Blank
            else if (stat.Mode == 1)
            {
                elapsedCpuCycles = ExecuteMode(
                    vblankClocks,
                    stat.IsVblankInterruptEnabled,
                    DrawFrame,
                    () =>
                    {
                        ly.Reset();
                        isFrameDone = true;
                        currentFrame++;
                    },
                    () => { ly.Set(screenHeight + (cyclesInMode / 456)); }
                );
            }
        }

        return isFrameDone;
    }

    private ulong ExecuteMode(ulong endCycles, bool canInterrupt, Func onEnter = null, Func onExit = null, Func onTick = null)
    {
        if (prevMode != stat.Mode)
        {
            if (onEnter != null) onEnter();
            if (canInterrupt)
                bus.RequestInterrrupt(InterruptType.LCDC);
        }

        if (cyclesInMode >= endCycles)
        {
            cyclesInMode -= endCycles;
            if (onExit != null) onExit();
            SetNextMode();
            return cyclesInMode;
        }
        else
        {
            if (onTick != null) onTick();
            prevMode = stat.Mode;
            return 0;
        }
    }


    private void SetNextMode()
    {
        byte mode = stat.Mode;
        SetMode(mode switch
        {
            0 => ly.Y == screenHeight ? 1 : 2,
            1 => 2,
            2 => 3,
            3 => 0,
            _ => throw new Exception("Impossible")
        });
    }

    private void SetMode(Byte mode)
    {
        prevMode = stat.Mode;
        stat.Mode = mode;
    }


    private Sprite[] spriteAttributes = new Sprite[40];

    private void LoadSprites()
    {
        Address address = OAM_StartAddress;
        int index = 0;
        while (address < OAM_EndAddress)
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
        ushort bgAddress = lcdc.IsBgTileMap1 ? TileMap1Address : TileMap0Address;

        ushort wAddress = lcdc.IsWindowTileMap1 ? TileMap1Address : TileMap0Address;

        // load bg and w tilemaps
        for (ushort i = 0; i < mapSize; i++)
        {
            byte tile = Read(bgAddress + i);
            backgroundMap[i] = tile;

            //might need to change this to skip tiles 20 - 32 per row
            if (i < windowMap.Length)
            {
                tile = Read(wAddress + i);
                windowMap[i] = tile;
            }
        }
    }

    private const byte tilesPerRow = 32;
    private const byte tileRows = 32;

    private byte[] pixels = new byte[screenWidth * screenHeight];

    private Tile GetTileAtPoint(Byte x, Byte y, byte[] map, Byte width)
    {
        Address index = (y / 8) * width + (x / 8);
        return LoadTilePattern(map[index]);
    }

    private Tile GetBackgroundTileAtPoint(Byte x, Byte y)
    {
        if (!lcdc.IsBackgroundEnabled) return new Tile();
        return GetTileAtPoint(x, y, backgroundMap, tilesPerRow);
    }
    private Tile GetWindowTile(Byte x, Byte y)
    {
        if (!lcdc.IsWindowEnabled) return new Tile();
        return GetTileAtPoint(x, y, windowMap, 20);
    }


    private void PrepareLine()
    {
        int firstPixelIndex = ly.Y * screenWidth;

        Byte y = scy.Read() + ly.Y;
        Byte x = scx.Read();

        Byte tileY = y % 8;
        Tile tile = GetBackgroundTileAtPoint(x, y);
        for (int i = 0; i < screenWidth; i++)
        {
            Byte tileX = x % 8;
            Byte colorCode = tile.GetColorCode(tileX, tileY);
            pixels[i + firstPixelIndex] = bgp.DecodeColorNumber(colorCode);
            x++;
            if (tileX == 7)
            {
                tile = GetBackgroundTileAtPoint(x, y);
            }
        }

        //window
        if (lcdc.IsWindowEnabled)
        {
            Byte yWindow = ly.Y - wy.Read();
            tileY = yWindow % 8;

            Byte xWindow = wx.Read();
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
                pixels[i + firstPixelIndex] = bgp.DecodeColorNumber(colorCode);
                xWindow++;
                if (tileX == 7)
                {
                    tile = GetWindowTile(xWindow, yWindow);
                }
            }
        }

        //sprites
        if (lcdc.IsSpritesEnabled)
        {
            int spriteHeight = lcdc.IsDoubleSpriteSize ? 16 : 8;
            foreach (Sprite s in spriteAttributes)
            {
                int screenY = s.Y - 16;
                if (screenY <= ly.Y && ly.Y < screenY + spriteHeight) // LY is somewhere within the sprite
                {
                    Palette palette = s.Palette ? obp1 : obp0;
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
                    int ty = ly.Y - screenY;
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

        bool isSignedIndex = !lcdc.IsBgAndWTileData1;
        ushort startAddress = lcdc.IsBgAndWTileData1 ? TileData1 : TileData0;
        ushort offset = isSignedIndex ? (ushort)(((sbyte)patternIndex - sbyte.MinValue) * 16) : (ushort)(patternIndex * 16);
        byte[] data = new byte[16];
        ushort address = (ushort)(startAddress + offset);
        for (int i = 0; i < 16; i++)
        {
            data[i] = Read(address++);
        }
        return new Tile(data);
    }

}
