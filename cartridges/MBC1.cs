class MBC1 : Cartridge
{
    private Byte mode;

    private Byte ROMBankNumber;
    private Byte RAMBankNumber;

    public MBC1(bool hasRAM, ushort ROMbanks, ushort RAMSize, byte[] cartridgeData)
    {
        mode = 0;

        Byte[] bankdata = GetCartridgeChunk(0, ROMSizePerBank, cartridgeData);
        romBank0 = new Mbc1Rom(bankdata, ToggleRAM, SetROMBankNr);

        // adjust for "holes" in bankspace at(0x20, 0x40, 0x60)
        if (ROMbanks == 63)
        {
            ROMbanks--;
        }
        else if (ROMbanks == 127)
        {
            ROMbanks -= 3;
        }

        IMemoryRange[] switchableBanks = new IMemoryRange[ROMbanks];
        for (int i = 0; i < ROMbanks; i++)
        {
            int startAddress = ROMSizePerBank * (i + 1);
            bankdata = GetCartridgeChunk(startAddress, ROMSizePerBank, cartridgeData);
            switchableBanks[i] = new Mbc1Rom(bankdata, SetRAMBankNr, ToggleMode);
        }
        romBankN = new Bank(switchableBanks);

        if (hasRAM)
        {
            byte count;
            ushort size;
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
            ramBankN = new MbcRam(count, size);
        }
        else ramBankN = new MbcRam();
    }

    private void ToggleRAM(Byte value) => ((MbcRam)ramBankN).isEnabled = value == 0x0A;

    private void ToggleMode(Byte value)
    {
        mode = value & 1;
        UpdateBanks();
    }

    private void SetROMBankNr(Byte value)
    {
        ROMBankNumber = value & 0x1F;
        UpdateBanks();
    }

    private void SetRAMBankNr(Byte value)
    {
        RAMBankNumber = value & 3;
        UpdateBanks();
    }

    private void UpdateBanks()
    {
        if (mode == 1)
        {
            ((Bank)romBankN).Switch(CorrectedROMBankNumber(ROMBankNumber));
            ((Bank)ramBankN).Switch(RAMBankNumber);
        }
        else
        {
            ((Bank)ramBankN).Switch(0);
            Byte concattedNr = (RAMBankNumber << 5) | ROMBankNumber;
            ((Bank)romBankN).Switch(CorrectedROMBankNumber(concattedNr));
        }
    }

    private Byte CorrectedROMBankNumber(Byte value)
    {
        if (ROMBankNumber > 0x60) value -= 4;
        else if (ROMBankNumber > 0x40) value -= 3;
        else if (ROMBankNumber > 0x20) value -= 2;
        else if (ROMBankNumber > 0) value -= 1;
        return value;
    }
}

class MbcRam : Bank
{
    public bool isEnabled = false;

    public MbcRam(byte count, ushort size) : base(count, size) { }
    public MbcRam() : base(0, 0) { }
    public MbcRam(IMemoryRange[] banks) : base(banks) { }

    public override void Write(Address address, Byte value)
    {
        if (isEnabled) base.Write(address, value);
    }
}

class Mbc1Rom : MemoryRange
{
    public Mbc1Rom(Byte[] memory, WriteTrigger first = null, WriteTrigger second = null) : base(memory, true) => (OnFirstHalf, OnSecondHalf) = (first, second);

    public delegate void WriteTrigger(Byte value);

    public WriteTrigger OnFirstHalf;
    public WriteTrigger OnSecondHalf;

    public override void Write(Address address, Byte value)
    {
        if (address < 0x2000)
        {
            if (OnFirstHalf != null) OnFirstHalf(value);
        }
        else
        {
            if (OnSecondHalf != null) OnSecondHalf(value);
        }
    }
}