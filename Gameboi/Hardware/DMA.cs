using static GeneralMemoryMap;
using static MiscSpecialAddresses;

class DMA : Hardware
{
    private Register dma;

    public DMA() => dma = new WriteTriggerRegister(StartTransfer);

    private bool inProgress = false;
    private const byte clocksPerByte = 4;

    public override void Connect(Bus bus)
    {
        base.Connect(bus);
        bus.ReplaceMemory(DMA_address, dma);
    }

    private void StartTransfer(Byte value)
    {
        inProgress = true;
        target = OAM_StartAddress;
        source = value * 0x100;
    }

    private Address target;
    private Address source;

    public void Tick(byte elapsedCpuClocks)
    {
        if (inProgress)
        {
            while (elapsedCpuClocks >= clocksPerByte)
            {
                Write(target++, Read(source++));
                elapsedCpuClocks -= clocksPerByte;

                if (target == OAM_EndAddress)
                {
                    inProgress = false;
                    break;
                }
            }
        }
    }
}