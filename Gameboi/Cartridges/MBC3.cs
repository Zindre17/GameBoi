using Gameboi.Memory;

namespace Gameboi.Cartridges
{
    public class MBC3 : Mbc
    {
        private readonly int romBankCount;
        public MBC3(string romPath, bool hasRam, int romBankCount, RamSize ramSize, byte[] cartridgeData) : base(romPath)
        {
            this.romBankCount = romBankCount;
            MemoryRange[] switchableBanks = new MemoryRange[romBankCount];
            for (int i = 0; i < romBankCount; i++)
            {
                int startAddress = RomSizePerBank * i;
                var bankData = GetCartridgeChunk(startAddress, RomSizePerBank, cartridgeData);
                switchableBanks[i] = new MemoryRange(bankData, true);
            }
            romBanks = new Bank(switchableBanks);
            romBanks.Switch(1);

            byte count;
            ushort size;
            if (!hasRam)
            {
                count = 0;
                size = 0;
            }
            else
            {
                count = ramSize.Banks;
                size = ramSize.SizePerBank;
            }

            IMemoryRange[] ramAndClock = new IMemoryRange[0xD];
            var clock = new RTC();
            for (int i = 0; i < ramAndClock.Length; i++)
            {
                if (i > 0x07)
                {
                    ramAndClock[i] = clock;
                }
                else
                {
                    if (i <= count)
                        ramAndClock[i] = new MemoryRange(size);
                    else
                        ramAndClock[i] = new DummyRange();
                }
            }
            ramBanks = new MbcRam(ramAndClock, GetSaveFilePath());
        }

        private void ToggleRam(Byte value)
        {
            if (value == 0x0A)
                ((MbcRam)ramBanks).isEnabled = true;
            else if (value == 0)
                ((MbcRam)ramBanks).isEnabled = false;
        }
        private void SetRomBankNr(Byte value)
        {
            var bankNr = value & 0x7F;

            // Selecting bank 0 translates to bank 1
            if (bankNr == 0)
                bankNr = 1;

            romBanks.Switch(bankNr % romBankCount);
        }

        private void SetRamBankNr(Byte value)
        {
            if (value < 0x0D)
                ramBanks.Switch(value);
            if (value > 0x07)
                ((RTC)ramBanks.GetBank(value)).SetPointer(value - 0x08);
        }

        protected override void OnBank0Write(Address address, Byte value)
        {
            if (address < RomSizePerBank / 2)
                ToggleRam(value);
            else
                SetRomBankNr(value);
        }

        protected override void OnBank1Write(Address address, Byte value)
        {
            if (address < RomSizePerBank / 2)
                SetRamBankNr(value);
        }
    }
}
