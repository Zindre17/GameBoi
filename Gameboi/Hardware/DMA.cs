using GB_Emulator.Gameboi.Memory;
using static GB_Emulator.Statics.GeneralMemoryMap;
using static GB_Emulator.Statics.MiscSpecialAddresses;

namespace GB_Emulator.Gameboi.Hardware
{
    public class DMA : Hardware, IUpdateable
    {
        private readonly Register dma;

        public DMA() => dma = new WriteTriggerRegister(StartTransfer);

        private bool inProgress = false;
        private const byte clocksPerByte = 1;

        public override void Connect(Bus bus)
        {
            base.Connect(bus);
            bus.ReplaceMemory(DMA_address, dma);
        }

        private void StartTransfer(Byte value)
        {
            bus.SetCpuReadFilter(AddressLock);
            bus.SetCpuWriteFilter(AddressLock);
            inProgress = true;
            target = OAM_StartAddress;
            source = value * 0x100;
            unusedCycles = 0;
        }

        private bool AddressLock(Address address)
        {
            return address == DMA_address || (address >= HRAM_StartAddress && address < HRAM_EndAddress);
        }

        private Address target;
        private Address source;

        private byte unusedCycles;
        public void Update(byte cycles, ulong _)
        {
            if (inProgress)
            {
                unusedCycles += cycles;
                while (unusedCycles >= clocksPerByte)
                {
                    Write(target++, Read(source++));
                    unusedCycles -= clocksPerByte;

                    if (target == OAM_EndAddress)
                    {
                        inProgress = false;
                        bus.ClearCpuReadFilter();
                        bus.ClearCpuWriteFilter();
                        break;
                    }
                }
            }
        }
    }
}