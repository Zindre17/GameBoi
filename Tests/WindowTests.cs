using Gameboi.Cartridges;
using Gameboi.OpenGL;

namespace Tests;

[TestClass]
public class WindowTests
{
    [TestMethod]
    public void TestBackground()
    {
        var window = new Window();

        var rom = new byte[0x8000];

        RomHelper.SetTitle(rom, "Test Background");
        RomHelper.Enfeeble(rom);

        window.ChangeGame(RomReader.ReadRom(rom));

        var state = window.State;

        // Color 3 = black
        // Color 2 = dark gray
        // Color 1 = ligh gray
        // Color 0 = white
        state.BackgroundPalette = 0b11_10_01_00;

        // Setup a tile with horizontal lines at index 0
        // (all tiles in tilemap for background point to tile 0)
        for (var i = 0; i < 16; i += 2)
        {
            var color = i / 4;

            state.VideoRam[i + 16] = color switch
            {
                0 => 0,
                1 => 0,
                2 => 0xff,
                3 => 0xff,
                _ => throw new Exception()
            };

            state.VideoRam[i + 17] = color switch
            {
                0 => 0,
                1 => 0xff,
                2 => 0,
                3 => 0xff,
                _ => throw new Exception()
            };
        }

        state.VideoRam[0x1800] = 1;
        state.VideoRam[0x1801] = 1;
        state.VideoRam[0x1812] = 1;
        state.VideoRam[0x1813] = 1;
        state.VideoRam[0x1820] = 1;
        state.VideoRam[0x1833] = 1;
        state.VideoRam[0x1a00] = 1;
        state.VideoRam[0x1a13] = 1;
        state.VideoRam[0x1a20] = 1;
        state.VideoRam[0x1a21] = 1;
        state.VideoRam[0x1a32] = 1;
        state.VideoRam[0x1a33] = 1;

        window.Run();
    }

    [TestMethod]
    public void TestWindow()
    {
        var window = new Window();

        var rom = new byte[0x8000];

        RomHelper.SetTitle(rom, "Test Window");
        RomHelper.Enfeeble(rom);

        window.ChangeGame(RomReader.ReadRom(rom));

        var state = window.State;

        // Lcd on, wnd high tilemap, wnd on, bg/wnd low data area, bg+wnd on
        state.LcdControl = 0b1111_0001;

        state.BackgroundPalette = 0b11_10_01_00;
        state.ScrollY = 20;
        state.WindowX = 7;
        state.WindowY = 0;

        var tileStartIndex = 16;
        for (var i = 0; i < 16; i++)
        {
            var index = tileStartIndex + i;
            state.VideoRam[index] = 0xff;
        }

        // Add black corners
        state.VideoRam[0x1c00] = 1;
        state.VideoRam[0x1c01] = 1;
        state.VideoRam[0x1c12] = 1;
        state.VideoRam[0x1c13] = 1;
        state.VideoRam[0x1c20] = 1;
        state.VideoRam[0x1c33] = 1;
        state.VideoRam[0x1e00] = 1;
        state.VideoRam[0x1e13] = 1;
        state.VideoRam[0x1e20] = 1;
        state.VideoRam[0x1e21] = 1;
        state.VideoRam[0x1e32] = 1;
        state.VideoRam[0x1e33] = 1;

        window.Run();
    }
}

[TestClass]
public class SpriteLayerTests
{
    [TestMethod]
    public void MovingSprite()
    {
        var window = new Window();
        var state = window.State;

        var rom = new byte[0x8000];
        RomHelper.Enfeeble(rom);
        RomHelper.SetTitle(rom, "Sprites moving");

        window.ChangeGame(RomReader.ReadRom(rom));

        state.LcdControl = 0b1001_0111;
        state.ObjectPalette0 = 0b11_10_01_00;

        state.Oam[0] = 20;
        state.Oam[1] = 20;
        state.Oam[2] = 2;
        state.Oam[3] = 0;

        for (var i = 1; i < 16; i += 2)
        {
            state.VideoRam[32 + i] = 0xff;
            state.VideoRam[32 + i - 1] = 0xe7;
        }

        for (var i = 0; i < 16; i += 2)
        {
            state.VideoRam[48 + i] = 0xff;
            state.VideoRam[48 + i + 1] = 0xe7;
        }

        var counter = 0;
        window.OnFrameUpdate += () =>
        {
            state.Oam[1] += 1;
            state.Oam[1] %= 167;
            state.Oam[0] += 1;
            state.Oam[0] %= 159;
            counter++;
            if (counter > 120)
            {
                counter = 0;
                state.LcdControl ^= 0b0000_0100;
            }
        };

        window.Run();
    }

    [TestMethod]
    public void ColorBackground()
    {
        var window = new Window();

        var rom = new byte[0x8000];

        RomHelper.SetColorMode(rom);
        RomHelper.SetTitle(rom, "Test Background");
        RomHelper.Enfeeble(rom);

        window.ChangeGame(RomReader.ReadRom(rom));

        var state = window.State;

        ushort color = 0b0_11111_00000_11111;

        state.BackgroundColorPaletteData[8] = (byte)(color & 0xff);
        state.BackgroundColorPaletteData[9] = (byte)((color >> 8) & 0xff);

        state.VideoRam[0x3800] = 1;
        state.VideoRam[0x3801] = 1;
        state.VideoRam[0x3812] = 1;
        state.VideoRam[0x3813] = 1;
        state.VideoRam[0x3820] = 1;
        state.VideoRam[0x3833] = 1;
        state.VideoRam[0x3a00] = 1;
        state.VideoRam[0x3a13] = 1;
        state.VideoRam[0x3a20] = 1;
        state.VideoRam[0x3a21] = 1;
        state.VideoRam[0x3a32] = 1;
        state.VideoRam[0x3a33] = 1;

        window.Run();
    }
}

public static class RomHelper
{
    public static void Enfeeble(byte[] rom)
    {
        // Entrypoint:
        // Make sure program does nothing (inf. loop)
        rom[0x100] = 0; // NOP
        rom[0x101] = 0xc3; // Jump 0x0100;
        rom[0x102] = 0;
        rom[0x102] = 1;

        // V-blank interrupt
        rom[0x40] = 0; // NOP
        rom[0x41] = 0xc3; // Jump 0x0100;
        rom[0x42] = 0;
        rom[0x43] = 1;

        // Lcd status interrupt
        rom[0x48] = 0; // NOP
        rom[0x49] = 0xc3; // Jump 0x0100;
        rom[0x4a] = 0;
        rom[0x4b] = 1;

        // Timer interrupt
        rom[0x50] = 0; // NOP
        rom[0x51] = 0xc3; // Jump 0x0100;
        rom[0x52] = 0;
        rom[0x53] = 1;

        // Serial interrupt
        rom[0x58] = 0; // NOP
        rom[0x59] = 0xc3; // Jump 0x0100;
        rom[0x5a] = 0;
        rom[0x5b] = 1;

        // Joypad interrupt
        rom[0x60] = 0; // NOP
        rom[0x61] = 0xc3; // Jump 0x0100;
        rom[0x62] = 0;
        rom[0x63] = 1;
    }

    public static void SetTitle(byte[] rom, string title)
    {
        var currentPosition = 0x134;
        foreach (var c in title)
        {
            rom[currentPosition] = (byte)c;
            currentPosition++;
            if (currentPosition is 0x144)
            {
                break;
            }
        }
    }

    internal static void SetColorMode(byte[] rom)
    {
        rom[0x143] = 0x80;
    }
}
