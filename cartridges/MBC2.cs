using System;
using static ByteOperations;

class MBC2 : Cartridge
{


    public MBC2(string romPath, byte ROMBanks, byte[] cartridgeData) : base(romPath)
    {
        if (ROMBanks > 15) throw new ArgumentException();

        Byte[] bankData = GetCartridgeChunk(0, ROMSizePerBank, cartridgeData);
        romBank0 = new Mbc2Rom(bankData, ToggleRAM, SetROMBankNr);

        IMemoryRange[] switchableBanks = new IMemoryRange[ROMBanks];
        for (int i = 0; i < ROMBanks; i++)
        {
            int startAddress = ROMSizePerBank * (i + 1);
            bankData = GetCartridgeChunk(startAddress, ROMSizePerBank, cartridgeData);
            switchableBanks[i] = new MemoryRange(bankData, true);
        }
        romBankN = new Bank(switchableBanks);

        ramBankN = new Mbc2Ram(GetSaveFilePath());
    }


    private void ToggleRAM(Address address)
    {
        Byte highByte = GetHighByte(address);
        if (!highByte[0])
            ((MbcRam)ramBankN).isEnabled = !((MbcRam)ramBankN).isEnabled;
    }

    private void SetROMBankNr(Address address, Byte value)
    {
        Byte highByte = GetHighByte(address);
        if (highByte[0])
            ((Bank)romBankN).Switch(value & 0x0F);
    }

}

class Mbc2Ram : MbcRam
{
    private const ushort RAMSize = 0x200;
    public Mbc2Ram(string saveFileName) : base(null, saveFileName)
    {
        banks = new IMemoryRange[1];

        var ram = new IMemory[0x2000];
        for (int i = 0; i < 0x2000; i++)
        {
            if (i < RAMSize)
                ram[i] = new MaskedRegister(0xF0);
            else ram[i] = new Dummy();
        }
        banks[0] = new MemoryRange(ram);
    }
}

class Mbc2Rom : MemoryRange
{
    public Mbc2Rom(Byte[] memory, WriteTriggerFirst first = null, WriteTriggerSecond second = null) : base(memory, true) => (OnFirstHalf, OnSecondHalf) = (first, second);

    public delegate void WriteTriggerFirst(Address value);
    public delegate void WriteTriggerSecond(Address address, Byte value);
    public WriteTriggerFirst OnFirstHalf;
    public WriteTriggerSecond OnSecondHalf;

    public override void Write(Address address, Byte value, bool isCpu = false)
    {
        if (address < 0x2000)
        {
            if (OnFirstHalf != null) OnFirstHalf(address);
        }
        else
        {
            if (OnSecondHalf != null) OnSecondHalf(address, value);
        }
    }
}