using GB_Emulator.Memory;

namespace GB_Emulator.Cartridges
{
    public class MBC5 : Mbc
    {

        private int romBankCount;
        public MBC5(string romPath, bool hasRam, int romBankCount, RamSize ramSize, byte[] cartridgeData) : base(romPath)
        {
            this.romBankCount = romBankCount;
            MemoryRange[] switchableBanks = new MemoryRange[romBankCount];
            for (int i = 0; i < switchableBanks.Length; i++)
            {
                int startAddress = RomSizePerBank * i;
                var bankData = GetCartridgeChunk(startAddress, RomSizePerBank, cartridgeData);
                switchableBanks[i] = new MemoryRange(bankData, true);
            }
            romBanks = new Bank(switchableBanks);
            romBanks.Switch(1);

            if (hasRam)
                ramBanks = new MbcRam(ramSize.Banks, ramSize.SizePerBank, GetSaveFilePath());
            else
                ramBanks = new Bank(0, 0);
        }

        private const ushort QuarterBank = RomSizePerBank / 4;

        private Byte lowerRomSelect;
        private Byte upperRomSelect;

        private Byte ramSelect;

        protected override void OnBank0Write(Address address, Byte value)
        {
            if (address < QuarterBank * 2)
            {
                if (ramBanks is MbcRam ram)
                {
                    if (value == 0x0A)
                        ram.isEnabled = true;
                    else if (value == 0)
                        ram.isEnabled = false;
                }
                return;
            }
            else if (address < QuarterBank * 3)
                lowerRomSelect = value;
            else
                upperRomSelect = value & 1;

            var bankNr = upperRomSelect << 8 | lowerRomSelect;
            romBanks.Switch(bankNr % romBankCount);
        }

        protected override void OnBank1Write(Address address, Byte value)
        {
            if (address < QuarterBank * 2)
                ramSelect = value & 0x0F;

            ramBanks.Switch(ramSelect);
        }
    }
}
