using System;
using System.IO;

namespace Gameboi.Cartridges;

public static class RomReader
{
    private const int romBanksPosition = 0x148;
    private const int ramSizePosition = 0x149;
    private const int cartridgeTypeAddress = 0x147;

    public static RomCartridge ReadRom(string filePath)
    {
        byte[] allBytes = File.ReadAllBytes(filePath);

        var romBanks = TranslateRomSizeTypeToBanks(allBytes[romBanksPosition]);
        var rom = new byte[romBanks * 0x4000];
        Array.Copy(allBytes, 0, rom, 0, romBanks * 0x4000);

        var (type, hasRam) = GetCartridgeType(allBytes[cartridgeTypeAddress]);

        RamSize ramSize;
        if (type is MbcType.Mbc2)
        {
            ramSize = new(1, 0x200);
        }
        else if (hasRam is false)
        {
            ramSize = new(0, 0);
        }
        else
        {
            ramSize = TranslateRamSize(allBytes[ramSizePosition]);
        }
        var ram = new byte[ramSize.Banks * ramSize.SizePerBank];

        return new(rom, ram, type);
    }

    public static RamSize TranslateRamSize(byte type)
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

    public static int TranslateRomSizeTypeToBanks(byte type)
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
            8 => 512,
            0x52 => 72,
            0x53 => 80,
            0x54 => 96,
            _ => throw new ArgumentException("Unexpected type"),
        };
    }

    public static (MbcType Type, bool HasRam) GetCartridgeType(byte value)
    {
        return value switch
        {
            0 => (MbcType.NoMbc, false),
            1 => (MbcType.Mbc1, false),
            2 or 3 => (MbcType.Mbc1, true),
            5 or 6 => (MbcType.Mbc2, false),
            8 or 9 => (MbcType.NoMbc, true),
            0x0f => (MbcType.Mbc3, false),
            0x10 => (MbcType.Mbc3, true),
            0x11 => (MbcType.Mbc3, false),
            0x12 or
            0x13 => (MbcType.Mbc3, true),
            0x19 => (MbcType.Mbc5, false),
            0x1a or
            0x1b => (MbcType.Mbc5, true),
            0x1c => (MbcType.Mbc5, false),
            0x1d => (MbcType.Mbc5, true),
            0x1e => (MbcType.Mbc5, true),
            _ => throw new Exception("Does not support cartridge type")
        };
    }
}

public record RomCartridge(byte[] Rom, byte[] Ram, MbcType Type);

public record RamSize(int Banks, int SizePerBank);
