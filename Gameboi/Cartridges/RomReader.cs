using System;
using System.IO;

namespace Gameboi.Cartridges;

public static class RomReader
{
    private const int romBanksPosition = 0x148;
    private const int ramSizePosition = 0x149;

    public static RomCartridge ReadRom(string filePath)
    {
        byte[] allBytes = File.ReadAllBytes(filePath);

        var romBanks = TranslateRomSizeTypeToBanks(allBytes[romBanksPosition]);
        var rom = new byte[romBanks * 0x4000];
        Array.Copy(allBytes, 0, rom, 0, romBanks * 0x4000);

        var ramSize = TranslateRamSize(allBytes[ramSizePosition]);
        var ram = new byte[ramSize.Banks * ramSize.SizePerBank];

        return new(rom, ram);
    }

    private static RamSize TranslateRamSize(byte type)
    {
        return type switch
        {
            0 => new RamSize(0, 0),
            1 => new RamSize(1, 0x500),
            2 => new RamSize(1, 0x2000),
            3 => new RamSize(4, 0x2000),
            4 => new RamSize(16, 0x2000),
            _ => throw new ArgumentException("Unexpected type"),
        };
    }

    private static int TranslateRomSizeTypeToBanks(byte type)
    {
        return type switch
        {
            0 => 2,
            1 => 4,
            2 => 8,
            3 => 16,
            4 => 32,
            5 => 64,
            6 => 128,
            7 => 256,
            0x52 => 72,
            0x53 => 80,
            0x54 => 96,
            _ => throw new ArgumentException("Unexpected type"),
        };
    }
}

public record RomCartridge(byte[] Rom, byte[] Ram);

public record RamSize(int Banks, int SizePerBank);
