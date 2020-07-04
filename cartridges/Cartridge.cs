using System;
using System.IO;
using System.Text;

abstract class Cartridge
{
    protected IMemoryRange romBank0;
    protected IMemoryRange romBankN;
    protected IMemoryRange ramBankN;

    public IMemoryRange RomBank0 => romBank0;
    public IMemoryRange RomBankN => romBankN;
    public IMemoryRange RamBankN => ramBankN;

    protected const ushort ROMSizePerBank = 0x4000;

    private string title;
    private const ushort cartridgeTypeAddress = 0x147;

    public static Cartridge LoadGame(string pathToROM)
    {
        //read rom file
        byte[] allBytes = File.ReadAllBytes(pathToROM);

        //get info from header
        Cartridge game = SetupCartridge(allBytes);

        game.isJapanese = allBytes[isJapaneseAddress] == 0;
        game.title = ReadTitle(allBytes);

        return game;
    }

    private const ushort ROMSizeAddress = 0x148;

    private const ushort RAMSizeAddress = 0x149;

    private const ushort isJapaneseAddress = 0x14A;
    private bool isJapanese;
    public bool IsJapanese => isJapanese;

    private const ushort titleStart = 0x134;
    private const ushort titleEnd = 0x143;
    private const byte titleLength = titleEnd + 1 - titleStart;
    private static string ReadTitle(byte[] allBytes)
    {
        byte[] titleBytes = new byte[titleLength];
        for (byte i = 0; i < titleLength; i++)
        {
            titleBytes[i] = allBytes[i + titleStart];
        }
        return Encoding.ASCII.GetString(titleBytes, 0, titleLength);
    }

    private static Cartridge SetupCartridge(byte[] allBytes)
    {
        Byte type = allBytes[cartridgeTypeAddress];
        byte ROMSizeType = allBytes[ROMSizeAddress];
        byte RAMSizeType = allBytes[RAMSizeAddress];
        byte ROMBanks = TranslateROMSizeTypeToBanks(ROMSizeType);
        ushort RAMSize = TranslateRAMSizeTypeToTotalSize(RAMSizeType);
        /*
        00h  ROM ONLY                 13h  MBC3+RAM+BATTERY
        01h  MBC1                     15h  MBC4
        02h  MBC1+RAM                 16h  MBC4+RAM
        03h  MBC1+RAM+BATTERY         17h  MBC4+RAM+BATTERY
        05h  MBC2                     19h  MBC5
        06h  MBC2+BATTERY             1Ah  MBC5+RAM
        08h  ROM+RAM                  1Bh  MBC5+RAM+BATTERY
        09h  ROM+RAM+BATTERY          1Ch  MBC5+RUMBLE
        0Bh  MMM01                    1Dh  MBC5+RUMBLE+RAM
        0Ch  MMM01+RAM                1Eh  MBC5+RUMBLE+RAM+BATTERY
        0Dh  MMM01+RAM+BATTERY        FCh  POCKET CAMERA
        0Fh  MBC3+TIMER+BATTERY       FDh  BANDAI TAMA5
        10h  MBC3+TIMER+RAM+BATTERY   FEh  HuC3
        11h  MBC3                     FFh  HuC1+RAM+BATTERY
        12h  MBC3+RAM
        */
        if (type == 0 || type == 8 || type == 9)
        {
            return new NoMBC(type != 0, allBytes);
        }
        else if (type > 0 && type < 4)
        {
            return new MBC1(type > 1, ROMBanks, RAMSize, allBytes);
        }
        else if (type == 5 || type == 6)
        {
            return new MBC2(ROMBanks, allBytes);
        }
        else if (type > 0xF && type < 0x14)
        {
            return new MBC3(type == 0x12 || type == 0x13, ROMBanks, RAMSize, allBytes);
        }
        else throw new ArgumentException($"Could not setup cartridge type: {type}");
    }

    private static byte TranslateROMSizeTypeToBanks(byte type)
    {
        // Note: 1 bank is allready subtracted due to bank0 always existing
        switch (type)
        {
            case 0: return 1;
            case 1: return 3;
            case 2: return 7;
            case 3: return 15;
            case 4: return 31;
            case 5: return 63;
            case 6: return 127;
            case 7: return 255;
            case 0x52: return 71;
            case 0x53: return 79;
            case 0x54: return 95;
            default:
                throw new ArgumentException();
        }
    }

    private static ushort TranslateRAMSizeTypeToTotalSize(byte type)
    {
        switch (type)
        {
            case 0: return 0;
            case 1: return 0x500;
            case 2: return 0x2000;
            case 3: return 0x8000;
            default:
                throw new ArgumentException();
        }
    }

    protected static Byte[] GetCartridgeChunk(int start, int size, byte[] allBytes)
    {
        Byte[] bytes = new Byte[size];
        for (int i = 0; i < size; i++)
        {
            if (i >= allBytes.Length) throw new ArgumentOutOfRangeException();
            bytes[i] = allBytes[start + i];
        }
        return bytes;
    }

}
