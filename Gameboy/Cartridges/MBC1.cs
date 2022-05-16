using GB_Emulator.Gameboi;
using GB_Emulator.Gameboi.Memory;
using static GB_Emulator.Statics.GeneralMemoryMap;

namespace GB_Emulator.Cartridges
{
    public class Mbc1 : Mbc
    {
        private Byte mode;

        private int RomBankNumber;
        private int RamBankNumber;

        private readonly int romBankCount;

        public Mbc1(string romPath, bool hasRam, int romBankCount, RamSize ramSize, byte[] cartridgeData) : base(romPath)
        {
            mode = 0;

            this.romBankCount = romBankCount;

            MemoryRange[] switchableBanks = new MemoryRange[romBankCount];
            for (int i = 0; i < romBankCount; i++)
            {
                int startAddress = RomSizePerBank * i;
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

            Bank0 = romBanks.GetBank(0);
        }

        public IMemoryRange Bank0 { get; set; }

        private Bus bus;
        public override void Connect(Bus bus)
        {
            this.bus = bus;
            bus.RouteMemory(ROM_bank_0_StartAddress, Bank0, OnBank0Write);
            bus.RouteMemory(ROM_bank_n_StartAddress, romBanks, OnBank1Write);
            bus.RouteMemory(ExtRAM_StartAddress, ramBanks, ExtRAM_EndAddress);
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
            if (mode == 1)
                Bank0 = romBanks.GetBank((RamBankNumber << 5) % romBankCount);
            else
                Bank0 = romBanks.GetBank(0);
            bus.RouteMemory(ROM_bank_0_StartAddress, Bank0, OnBank0Write);
        }

        private int GetRomBankPointer()
        {
            var nr = RamBankNumber << 5 | RomBankNumber;
            return CorrectedRomBankNumber(nr);
        }

        // Adjust for "holes" in bankspace at(0x20, 0x40, 0x60) and 0 -> 1
        private int CorrectedRomBankNumber(int value)
        {
            if (value == 0 || value == 0x20 || value == 0x40 || value == 0x60)
                value += 1;
            return value % romBankCount;
        }
    }
}