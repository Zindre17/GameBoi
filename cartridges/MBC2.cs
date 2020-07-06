using System;
using static ByteOperations;

class MBC2 : Cartridge
{
    private const ushort RAMSize = 0x200;

    public MBC2(byte ROMBanks, byte[] cartridgeData)
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

        ramBankN = new MbcRam(1, RAMSize);
    }


    private void ToggleRAM(Address address)
    {
        byte highByte = GetHighByte(address);
        if (TestBit(0, highByte))
            ((MbcRam)ramBankN).isEnabled = !((MbcRam)ramBankN).isEnabled;
    }

    private void SetROMBankNr(Address address, Byte value)
    {
        Byte highByte = GetHighByte(address);
        if (TestBit(0, highByte))
        {
            ((Bank)romBankN).Switch(value & 0x0F);
        }
    }

}

class Mbc2Rom : MemoryRange
{
    public Mbc2Rom(Byte[] memory, WriteTriggerFirst first = null, WriteTriggerSecond second = null) : base(memory, true) => (OnFirstHalf, OnSecondHalf) = (first, second);

    public delegate void WriteTriggerFirst(Address value);
    public delegate void WriteTriggerSecond(Address address, Byte value);
    public WriteTriggerFirst OnFirstHalf;
    public WriteTriggerSecond OnSecondHalf;

    public override void Write(Address address, Byte value)
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