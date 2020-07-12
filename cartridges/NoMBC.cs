using System;

class NoMBC : Cartridge
{
    private bool hasRAM = false;
    private bool hasBattery = false;

    public NoMBC(bool hasRAM, byte[] cartridgeData, byte[] batteryStoredRAM = null)
    {
        this.hasRAM = hasRAM;

        if (batteryStoredRAM != null)
            hasBattery = true;

        romBank0 = new MemoryRange(GetCartridgeChunk(0, ROMSizePerBank, cartridgeData), true);
        romBankN = new MemoryRange(GetCartridgeChunk(ROMSizePerBank, ROMSizePerBank, cartridgeData), true);

        if (hasRAM)
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
