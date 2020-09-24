class MBC5 : Mbc
{
    public MBC5(string romPath, bool hasRam, Byte romBanks, RamSize ramSize, byte[] cartridgeData) : base(romPath)
    {
        Byte[] bankData = GetCartridgeChunk(0, RomSizePerBank, cartridgeData);
        IMemory[] mem = new IMemory[bankData.Length];
        for (int i = 0; i < bankData.Length; i++)
            mem[i] = new Register(bankData[i], true);
        romBank0 = new MbcRom(mem, OnBank0Write);

        IMemoryRange[] switchableBanks = new IMemoryRange[romBanks];
        for (int i = 0; i < romBanks; i++)
        {
            if (i == 0)
                switchableBanks[i] = new MbcRom(mem, OnBank1Write);
            else
            {
                int startAddress = RomSizePerBank * (i);
                bankData = GetCartridgeChunk(startAddress, RomSizePerBank, cartridgeData);
                switchableBanks[i] = new MbcRom(bankData, OnBank1Write);
            }
        }
        romBankN = new Bank(switchableBanks);

        if (hasRam)
            ramBankN = new MbcRam(ramSize.Banks, ramSize.SizePerBank, GetSaveFilePath());
        else
            ramBankN = new Bank(0, 0);
    }

    private const ushort QuarterBank = RomSizePerBank / 4;

    private Byte lowerRomSelect;
    private Byte upperRomSelect;

    private Byte ramSelect;

    protected override void OnBank0Write(Address address, Byte value)
    {
        if (address < QuarterBank * 2)
        {

        }
        else if (address < QuarterBank * 3)
            lowerRomSelect = value;
        else
            upperRomSelect = value & 1;

        Address bankNr = (upperRomSelect << 8) | lowerRomSelect;
        ((Bank)romBankN).Switch(bankNr);
    }

    protected override void OnBank1Write(Address address, Byte value)
    {
        if (address < QuarterBank * 2)
            ramSelect = value & 0x0F;

        ((Bank)ramBankN).Switch(ramSelect);
    }
}