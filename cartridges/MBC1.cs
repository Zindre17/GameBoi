
class Mbc1 : Mbc
{
    private Byte mode;

    private Byte RomBankNumber;
    private Byte RamBankNumber;



    public Mbc1(string romPath, bool hasRam, Byte romBanks, RamSize ramSize, byte[] cartridgeData) : base(romPath)
    {
        mode = 0;

        Byte[] bankdata = GetCartridgeChunk(0, RomSizePerBank, cartridgeData);
        romBank0 = new MbcRom(bankdata, OnBank0Write);

        // adjust for "holes" in bankspace at(0x20, 0x40, 0x60)
        if (romBanks == 63)
            romBanks--;
        else if (romBanks == 127)
            romBanks -= 3;

        IMemoryRange[] switchableBanks = new IMemoryRange[romBanks];
        for (int i = 0; i < romBanks; i++)
        {
            int startAddress = RomSizePerBank * (i + 1);
            bankdata = GetCartridgeChunk(startAddress, RomSizePerBank, cartridgeData);
            switchableBanks[i] = new MbcRom(bankdata, OnBank1Write);
        }
        romBankN = new Bank(switchableBanks);

        if (hasRam)
        {
            ramBankN = new MbcRam(ramSize.Banks, ramSize.SizePerBank, GetSaveFilePath());
        }
        else ramBankN = new Bank(0, 0);
    }

    protected override void OnBank0Write(Address address, Byte value)
    {
        if (address < RomSizePerBank / 2)
            ((MbcRam)ramBankN).isEnabled = value == 0x0A;
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
        ((Bank)ramBankN).Switch(mode * RamBankNumber);
        ((Bank)romBankN).Switch(GetRomBankPointer());
    }

    private Byte GetRomBankPointer()
    {
        Byte nr;
        if (mode == 0)
        {
            nr = (RamBankNumber << 5) | RomBankNumber;
            nr = CorrectedRomBankNumber(nr);
        }
        else
        {
            nr = CorrectedRomBankNumber(RomBankNumber);
        }
        return nr;
    }

    private Byte CorrectedRomBankNumber(Byte value)
    {
        if (RomBankNumber > 0x60) value -= 4;
        else if (RomBankNumber > 0x40) value -= 3;
        else if (RomBankNumber > 0x20) value -= 2;
        else if (RomBankNumber > 0) value -= 1;
        return value;
    }
}
