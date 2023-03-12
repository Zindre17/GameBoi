using System;
using System.IO;

namespace Gameboi.Cartridges;

public static class RomReader
{
    private const int typePosition = 0x147;
    private const int romBanksPosition = 0x148;
    private const int ramSizePosition = 0x149;

    public static RomCartridge ReadRom(string filePath)
    {
        byte[] allBytes = File.ReadAllBytes(filePath);

        var type = InterpretCartridgeType(allBytes[typePosition]);

        var romBanks = TranslateRomSizeTypeToBanks(allBytes[romBanksPosition]);
        var rom = new byte[romBanks * 0x4000];
        Array.Copy(allBytes, 0, rom, 0, romBanks * 0x4000);

        var ramSize = TranslateRamSize(allBytes[ramSizePosition]);
        var ram = new byte[ramSize.Banks * ramSize.SizePerBank];

        return new(rom, ram, type);
    }

    private static IMemoryBankControllerLogic InterpretCartridgeType(byte typeValue)
    {
        return typeValue switch
        {
            0 or 8 or 9 => new NoMemoryBankController(),
            1 or 2 or 3 => new MemoryBankController1(),
            5 or 6 => new MemoryBankController2(),
            >= 0xF and <= 0x13 => new MemoryBankController3(),
            >= 0x19 and <= 0x1E => new MemoryBankController5(),

            _ => throw new Exception("Does not support cartridge type")
        };
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

public record RomCartridge(byte[] Rom, byte[] Ram, IMemoryBankControllerLogic Logic);

public record RamSize(int Banks, int SizePerBank);
