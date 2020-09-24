using System;

class MBC3 : Mbc
{
    //TODO: implement internal RTC-clock

    public MBC3(string romPath, bool hasRam, byte romBanks, RamSize ramSize, byte[] cartridgeData) : base(romPath)
    {
        Byte[] bankData = GetCartridgeChunk(0, RomSizePerBank, cartridgeData);
        romBank0 = new MbcRom(bankData, OnBank0Write);

        IMemoryRange[] switchableBanks = new IMemoryRange[romBanks];
        for (int i = 0; i < romBanks; i++)
        {
            int startAddress = RomSizePerBank * (i + 1);
            bankData = GetCartridgeChunk(startAddress, RomSizePerBank, cartridgeData);
            switchableBanks[i] = new MbcRom(bankData, OnBank1Write);
        }
        romBankN = new Bank(switchableBanks);


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
        ramBankN = new MbcRam(ramAndClock, GetSaveFilePath());

    }

    private void ToggleRam(Byte value)
    {
        ((MbcRam)ramBankN).isEnabled = value == 0x0A;
    }
    private void SetRomBankNr(Byte value)
    {
        Byte bankNr = value & 0x7F;
        if (bankNr > 0)
            bankNr--;
        ((Bank)RomBankN).Switch(bankNr);
    }

    private void SetRamBankNr(Byte value)
    {
        value &= 0x0F;
        if (value > 0x0C) throw new ArgumentOutOfRangeException();
        ((Bank)ramBankN).Switch(value);
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