using System;
using GB_Emulator.Gameboi.Memory;
using static GB_Emulator.Statics.ByteOperations;
using Byte = GB_Emulator.Gameboi.Memory.Byte;

namespace GB_Emulator.Cartridges
{
    public class Mbc2 : Mbc
    {
        public Mbc2(string romPath, int romBankCount, byte[] cartridgeData) : base(romPath)
        {
            if (romBankCount > 16) throw new ArgumentOutOfRangeException(nameof(romBankCount));

            MemoryRange[] switchableBanks = new MemoryRange[romBankCount];
            for (int i = 0; i < romBankCount; i++)
            {
                int startAddress = RomSizePerBank * i;
                var bankData = GetCartridgeChunk(startAddress, RomSizePerBank, cartridgeData);
                switchableBanks[i] = new MemoryRange(bankData, true);
            }

            romBanks = new Bank(switchableBanks);
            romBanks.Switch(1);
            ramBanks = new Mbc2Ram(GetSaveFilePath());
        }

        private void ToggleRam(Address address)
        {
            Byte highByte = GetHighByte(address);
            if (!highByte[0])
                ((MbcRam)ramBanks).isEnabled = !((MbcRam)ramBanks).isEnabled;
        }

        private void SetRomBankNr(Address address, Byte value)
        {
            Byte highByte = GetHighByte(address);
            if (highByte[0])
            {
                var bankNr = value & 0x0F;
                if (bankNr == 0)
                    bankNr = 1;
                romBanks.Switch(bankNr);
            }
        }

        protected override void OnBank0Write(Address address, Byte value)
        {
            if (address < RomSizePerBank / 2)
                ToggleRam(address);
            else
                SetRomBankNr(address, value);
        }

        protected override void OnBank1Write(Address address, Byte value) { }

    }

    public class Mbc2Ram : MbcRam
    {
        private const ushort RAMSize = 0x200;
        public Mbc2Ram(string saveFileName) : base(null, null)
        {
            banks = new IMemoryRange[1];

            var ram = new IMemory[0x2000];
            for (int i = 0; i < 0x2000; i++)
            {
                if (i < RAMSize)
                    ram[i] = new MaskedRegister(0xF0);
                else ram[i] = new Dummy();
            }
            banks[0] = new MemoryRange(ram);
            PrepareSaveFile(saveFileName);
        }
    }
}