using System;
using GB_Emulator.Gameboi.Memory;
using static GB_Emulator.Statics.ByteOperations;
using Byte = GB_Emulator.Gameboi.Memory.Byte;

namespace GB_Emulator.Cartridges
{
    public class Mbc2 : Mbc
    {
        private readonly int romBankCount;
        public Mbc2(string romPath, int romBankCount, byte[] cartridgeData) : base(romPath)
        {
            this.romBankCount = romBankCount;
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

        private void ToggleRam(Byte value)
        {
            var lowNibble = GetLowNibble(value);
            ((MbcRam)ramBanks).isEnabled = lowNibble == 0b1010;
        }

        private void SetRomBankNr(Byte value)
        {
            var bankNr = value & 0x0F;
            if (bankNr == 0)
                bankNr = 1;
            romBanks.Switch(bankNr % romBankCount);
        }

        protected override void OnBank0Write(Address address, Byte value)
        {
            Byte highByte = GetHighByte(address);
            if (highByte[0])
                SetRomBankNr(value);
            else
                ToggleRam(value);
        }

        protected override void OnBank1Write(Address address, Byte value) { }

    }

    public class Mbc2Ram : MbcRam
    {
        private const ushort RAMSize = 0x200;
        public Mbc2Ram(string saveFileName) : base(null, null)
        {
            banks = new IMemoryRange[1];

            var ram = new IMemory[RAMSize];
            for (int i = 0; i < RAMSize; i++)
                ram[i] = new MaskedRegister(0xF0);
            banks[0] = new MemoryRange(ram);
            PrepareSaveFile(saveFileName);
        }

        public override Byte Read(Address address, bool isCpu = false)
        {
            return base.Read(address % RAMSize, isCpu);
        }

        public override void Write(Address address, Byte value, bool isCpu = false)
        {
            base.Write(address % RAMSize, value, isCpu);
        }
    }
}