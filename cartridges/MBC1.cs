using System.IO;

class MBC1 : Cartridge
{
    private Byte mode;

    private Byte ROMBankNumber;
    private Byte RAMBankNumber;

    public MBC1(string romPath, bool hasRAM, Byte ROMbanks, ushort RAMSize, byte[] cartridgeData) : base(romPath)
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
            ramBankN = new MbcRam(count, size, GetSaveFilePath());
        }
        else ramBankN = new DummyRange();
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
        ((Bank)ramBankN).Switch(mode * RAMBankNumber);
        ((Bank)romBankN).Switch(GetRomBankPointer());
    }

    private Byte GetRomBankPointer()
    {
        Byte nr;
        if (mode == 0)
        {
            nr = (RAMBankNumber << 5) | ROMBankNumber;
            nr = CorrectedROMBankNumber(nr);
        }
        else
        {
            nr = CorrectedROMBankNumber(ROMBankNumber);
        }
        return nr;
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

    public MbcRam(byte count, ushort size, string saveFileName = null) : base(count, size) => PrepareSaveFile(saveFileName);
    public MbcRam(IMemoryRange[] banks, string saveFileName = null) : base(banks) => PrepareSaveFile(saveFileName);

    private FileStream file;

    private void PrepareSaveFile(string saveFileName)
    {
        if (string.IsNullOrEmpty(saveFileName)) return;

        if (File.Exists(saveFileName))
        {
            byte[] allBytes = File.ReadAllBytes(saveFileName);
            pointer = 0;
            bool first = true;
            for (int i = 0; i < allBytes.Length; i++)
            {
                Address relAdr = i % Size;
                if (relAdr == 0 && !first) pointer++;
                else first = false;
                base.Write(relAdr, allBytes[i]);
            }
        }
        else
        {
            byte[] bytes = new byte[GetTotalSize()];
            File.WriteAllBytes(saveFileName, bytes);
        }

        file = File.OpenWrite(saveFileName);
    }
    public void CloseFileStream() => file.Close();

    public override Byte Read(Address address, bool isCpu = false)
    {
        if (isEnabled) return base.Read(address, isCpu);
        return 0xFF;
    }
    public override void Write(Address address, Byte value, bool isCpu = false)
    {
        if (isEnabled)
        {
            base.Write(address, value, isCpu);
            int offset = 0;
            for (int i = 0; i < pointer; i++)
                offset += banks[i].Size;
            file.Position = offset + address;
            file.WriteByte(value);
        }
    }
}

class Mbc1Rom : MemoryRange
{
    public Mbc1Rom(Byte[] memory, WriteTrigger first = null, WriteTrigger second = null) : base(memory, true) => (OnFirstHalf, OnSecondHalf) = (first, second);

    public delegate void WriteTrigger(Byte value);

    public WriteTrigger OnFirstHalf;
    public WriteTrigger OnSecondHalf;

    public override void Write(Address address, Byte value, bool isCpu = false)
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