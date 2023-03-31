using Gameboi.Cartridges;
using Gameboi.Memory.Io;
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
}
