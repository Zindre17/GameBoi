using System;
using System.IO;
using System.Text;
using GB_Emulator.Gameboi;
using GB_Emulator.Gameboi.Memory;
using Byte = GB_Emulator.Gameboi.Memory.Byte;

namespace GB_Emulator.Cartridges
{
    public abstract class Cartridge
    {

        protected Bank romBanks;
        protected Bank ramBanks;

        protected const ushort RomSizePerBank = 0x4000;

        public string Title { get; set; }
        private const ushort cartridgeTypeAddress = 0x147;
        private const ushort colorModeAddress = 0x143; // 0x80 both | 0xC0 only GBC | else only GB

        public bool IsColorGame { get; set; }

        protected string romPath;

        protected Cartridge(string romPath) => this.romPath = romPath;

        public abstract void Connect(Bus bus);

        public static Cartridge LoadGame(string pathToROM)
        {
            //read rom file
            byte[] allBytes = File.ReadAllBytes(pathToROM);

            //get info from header
            Cartridge game = SetupCartridge(pathToROM, allBytes);
            game.IsColorGame = allBytes[colorModeAddress] != 0;
            game.isJapanese = allBytes[isJapaneseAddress] == 0;
            game.Title = ReadTitle(allBytes);

            return game;
        }

        public void CloseFileStream()
        {
            if (ramBanks is not MbcRam ram) return;
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
            var romBanks = TranslateRomSizeTypeToBanks(romSizeType);
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
                return new MBC3(romPath, type == 0x10 || type == 0x12 || type == 0x13, romBanks, ramSize, allBytes);
            }
            else if (type > 0x18 && type < 0x1F)
            {
                return new MBC5(romPath, type != 0x19 && type != 0x1C, romBanks, ramSize, allBytes);
            }

            else throw new ArgumentException($"Could not setup cartridge type: {type}");
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

        protected static Byte[] GetCartridgeChunk(int start, int size, byte[] allBytes)
        {
            if (size >= allBytes.Length) throw new ArgumentOutOfRangeException(nameof(size));

            Byte[] bytes = new Byte[size];
            for (int i = 0; i < size; i++)
            {
                bytes[i] = allBytes[start + i];
            }
            return bytes;
        }

    }
}