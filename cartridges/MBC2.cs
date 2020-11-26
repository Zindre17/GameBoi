using System;
using static ByteOperations;

public class Mbc2 : Mbc
{
    public Mbc2(string romPath, byte romBanks, byte[] cartridgeData) : base(romPath)
    {
        if (romBanks > 15) throw new ArgumentException();

        Byte[] bankData = GetCartridgeChunk(0, RomSizePerBank, cartridgeData);
        romBank0 = new MbcRom(bankData, OnBank0Write);

        IMemoryRange[] switchableBanks = new IMemoryRange[romBanks];
        for (int i = 0; i < romBanks; i++)
        {
            int startAddress = RomSizePerBank * (i + 1);
            bankData = GetCartridgeChunk(startAddress, RomSizePerBank, cartridgeData);
            switchableBanks[i] = new MemoryRange(bankData, true);
        }
        romBankN = new Bank(switchableBanks);

        ramBankN = new Mbc2Ram(GetSaveFilePath());
    }


    private void ToggleRam(Address address)
    {
        Byte highByte = GetHighByte(address);
        if (!highByte[0])
            ((MbcRam)ramBankN).isEnabled = !((MbcRam)ramBankN).isEnabled;
    }

    private void SetRomBankNr(Address address, Byte value)
    {
        Byte highByte = GetHighByte(address);
        if (highByte[0])
            ((Bank)romBankN).Switch(value & 0x0F);
    }

    protected override void OnBank0Write(Address address, Byte value)
    {
        if (address < RomSizePerBank / 2)
            ToggleRam(address);
        else
            SetRomBankNr(address, value);
    }

    protected override void OnBank1Write(Address address, Byte value) { }

}

public class Mbc2Ram : MbcRam
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