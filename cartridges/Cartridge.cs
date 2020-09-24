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

    protected const ushort RomSizePerBank = 0x4000;

    private string title;
    private const ushort cartridgeTypeAddress = 0x147;

    protected string romPath;

    protected Cartridge(string romPath) => this.romPath = romPath;

    public static Cartridge LoadGame(string pathToROM)
    {
        //read rom file
        byte[] allBytes = File.ReadAllBytes(pathToROM);

        //get info from header
        Cartridge game = SetupCartridge(pathToROM, allBytes);

        game.isJapanese = allBytes[isJapaneseAddress] == 0;
        game.title = ReadTitle(allBytes);

        return game;
    }

    public void CloseFileStream()
    {
        var ram = RamBankN as MbcRam;
        if (ram == null) return;

        ram.CloseFileStream();
    }

    public string GetSaveFilePath()
    {
        int indexOfLastDot = romPath.LastIndexOf('.');
        return romPath.Substring(0, indexOfLastDot) + ".sav";
    }

    private const ushort romSizeAddress = 0x148;

    private const ushort ramSizeAddress = 0x149;

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

    private static Cartridge SetupCartridge(string romPath, byte[] allBytes)
    {
        Byte type = allBytes[cartridgeTypeAddress];
        byte romSizeType = allBytes[romSizeAddress];
        byte ramSizeType = allBytes[ramSizeAddress];
        byte romBanks = TranslateRomSizeTypeToBanks(romSizeType);
        RamSize ramSize = TranslateRamSize(ramSizeType);
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
            return new NoMBC(romPath, type != 0, allBytes);
        }
        else if (type > 0 && type < 4)
        {
            return new Mbc1(romPath, type > 1, romBanks, ramSize, allBytes);
        }
        else if (type == 5 || type == 6)
        {
            return new Mbc2(romPath, romBanks, allBytes);
        }
        else if (type > 0xF && type < 0x14)
        {
            return new MBC3(romPath, type == 0x12 || type == 0x13, romBanks, ramSize, allBytes);
        }
        else if (type > 0x18 && type < 0x1F)
        {
            return new MBC5(romPath, type != 0x19 && type != 0x1C, romBanks, ramSize, allBytes);
        }

        else throw new ArgumentException($"Could not setup cartridge type: {type}");
    }

    private static byte TranslateRomSizeTypeToBanks(byte type)
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

    public class RamSize
    {
        public RamSize(byte banks, ushort sizePerBank)
        {
            Banks = banks;
            SizePerBank = sizePerBank;
        }

        public Byte Banks { get; set; }
        public Address SizePerBank { get; set; }
    }

    private static RamSize TranslateRamSize(byte type)
    {
        switch (type)
        {
            case 0: return new RamSize(0, 0);
            case 1: return new RamSize(1, 0x500);
            case 2: return new RamSize(1, 0x2000);
            case 3: return new RamSize(4, 0x2000);
            case 4: return new RamSize(16, 0x2000);
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
