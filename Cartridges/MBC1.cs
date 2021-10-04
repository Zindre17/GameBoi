using GB_Emulator.Gameboi.Memory;

namespace GB_Emulator.Cartridges
{
    public class Mbc1 : Mbc
    {
        private Byte mode;

        private int RomBankNumber;
        private int RamBankNumber;

        public Mbc1(string romPath, bool hasRam, int romBankCount, RamSize ramSize, byte[] cartridgeData) : base(romPath)
        {
            mode = 0;

            MemoryRange[] switchableBanks = new MemoryRange[romBankCount];
            int banksLoaded = 0;
            for (int i = 0; i < romBankCount; i++)
            {
                if (i == 0x20 || i == 0x40 || i == 0x60) continue;

                int startAddress = RomSizePerBank * banksLoaded++;
                var bankdata = GetCartridgeChunk(startAddress, RomSizePerBank, cartridgeData);
                switchableBanks[i] = new MemoryRange(bankdata, true);
            }
            romBanks = new Bank(switchableBanks);
            romBanks.Switch(1);

            if (hasRam)
            {
                ramBanks = new MbcRam(ramSize.Banks, ramSize.SizePerBank, GetSaveFilePath());
            }
            else ramBanks = new MbcRam(0, 0);
        }

        protected override void OnBank0Write(Address address, Byte value)
        {
            if (address < RomSizePerBank / 2)
                // A value of 0xXA will enable ram. Any other value will disable it.
                ((MbcRam)ramBanks).isEnabled = (value & 0x0F) == 0x0A;
            else
                RomBankNumber = value & 0x1F;
            UpdateBanks();
        }

        protected override void OnBank1Write(Address address, Byte value)
        {
            if (address < RomSizePerBank / 2)
                RamBankNumber = value & 3;
            else
                mode = value & 1;
            UpdateBanks();
        }

        private void UpdateBanks()
        {
            ramBanks.Switch(mode * RamBankNumber);
            romBanks.Switch(GetRomBankPointer());
        }

        private int GetRomBankPointer()
        {
            if (mode == 0)
            {
                var nr = RamBankNumber << 5 | RomBankNumber;
                return CorrectedRomBankNumber(nr);
            }
            else
            {
                return CorrectedRomBankNumber(RomBankNumber);
            }
        }

        // Adjust for "holes" in bankspace at(0x20, 0x40, 0x60) and 0 -> 1
        private static int CorrectedRomBankNumber(int value)
        {
            if (value == 0 || value == 0x20 || value == 0x40 || value == 0x60)
                return value + 1;
            return value;
        }
    }
}