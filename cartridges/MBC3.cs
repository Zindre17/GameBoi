using System;

class MBC3 : Cartridge
{
    //TODO: implement internal RTC-clock

    public MBC3(bool hasRAM, byte ROMBanks, ushort RAMSize, byte[] cartridgeData)
    {
        Byte[] bankData = GetCartridgeChunk(0, ROMSizePerBank, cartridgeData);
        romBank0 = new Mbc1Rom(bankData, ToggleRAM, SetROMBankNr);

        IMemoryRange[] switchableBanks = new IMemoryRange[ROMBanks];
        for (int i = 0; i < ROMBanks; i++)
        {
            int startAddress = ROMSizePerBank * (i + 1);
            bankData = GetCartridgeChunk(startAddress, ROMSizePerBank, cartridgeData);
            switchableBanks[i] = new Mbc1Rom(bankData, SetRAMBankNr);
        }
        romBankN = new Bank(switchableBanks);


        byte count;
        ushort size;
        if (hasRAM)
        {
            count = 0;
            size = 0;
        }
        else
        {
            if (RAMSize == 0x8000)
            {
                count = 4;
                size = 0x2000;
            }
            else
            {
                count = 1;
                size = RAMSize;
            }
        }

        IMemoryRange[] ramAndClock = new IMemoryRange[0xC];
        for (int i = 0; i < 0xC; i++)
        {
            if (i > 3)
            {
                ramAndClock[i] = new MemoryRange(new Register());
            }
            else
            {
                if (i <= count)
                    ramAndClock[i] = new MemoryRange(size);
                else
                    ramAndClock[i] = null;
            }
        }
        ramBankN = new MbcRam(ramAndClock);

    }

    private void ToggleRAM(Byte value)
    {
        ((MbcRam)ramBankN).isEnabled = value == 0x0A;
    }
    private void SetROMBankNr(Byte value)
    {
        byte bankNr = value & 0x7F;
        if (bankNr > 0)
            bankNr--;
        ((Bank)RomBankN).Switch(bankNr);
    }

    private void SetRAMBankNr(Byte value)
    {
        value &= 0x0F;
        if (value > 0x0C) throw new ArgumentOutOfRangeException();
        ((Bank)ramBankN).Switch(value);
    }


}