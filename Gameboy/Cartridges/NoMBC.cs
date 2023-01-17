using System;
using GB_Emulator;
using GB_Emulator.Memory;
using static GB_Emulator.Statics.GeneralMemoryMap;

namespace GB_Emulator.Cartridges
{
    public class NoMBC : Cartridge
    {
        private readonly bool hasBattery = false;

        public NoMBC(string romPath, bool hasRam, byte[] cartridgeData, byte[] batteryStoredRam = null) : base(romPath)
        {
            if (batteryStoredRam != null)
                hasBattery = true;

            romBanks = new Bank(new IMemoryRange[]{
                new MemoryRange(GetCartridgeChunk(0, RomSizePerBank, cartridgeData), true),
                new MemoryRange(GetCartridgeChunk(RomSizePerBank, RomSizePerBank, cartridgeData), true)
            });

            if (hasRam)
            {
                if (hasBattery)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    ramBanks = new Bank(1, 0x2000);
                }
            }
            else
            {
                ramBanks = new Bank(0, 0);
            }
        }

        public override void Connect(Bus bus)
        {
            bus.RouteMemory(ROM_bank_0_StartAddress, romBanks.GetBank(0));
            bus.RouteMemory(ROM_bank_n_StartAddress, romBanks.GetBank(1));
            bus.RouteMemory(ExtRAM_StartAddress, ramBanks, ExtRAM_EndAddress);
        }
    }
}
