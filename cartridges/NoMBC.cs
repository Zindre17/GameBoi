using System;

public class NoMBC : Cartridge
{
    private readonly bool hasBattery = false;

    public NoMBC(string romPath, bool hasRam, byte[] cartridgeData, byte[] batteryStoredRam = null) : base(romPath)
    {
        if (batteryStoredRam != null)
            hasBattery = true;

        romBank0 = new MemoryRange(GetCartridgeChunk(0, RomSizePerBank, cartridgeData), true);
        romBankN = new MemoryRange(GetCartridgeChunk(RomSizePerBank, RomSizePerBank, cartridgeData), true);

        if (hasRam)
        {
            if (hasBattery)
            {
                throw new NotImplementedException();
            }
            else
            {
                ramBankN = new MemoryRange(0x2000);
            }
        }
        else
        {
            ramBankN = new DummyRange();
        }
    }
}
