namespace Gameboi.Hardware;

public class Dma
{
    private readonly SystemState state;
    private readonly ImprovedBus bus;

    private const byte ByteTransferCount = 0xa0;
    private const byte TicksPerTransfer = 4;
    private const int DmaDurationInTicks = ByteTransferCount * TicksPerTransfer;

    public Dma(SystemState state, ImprovedBus bus)
    {
        this.state = state;
        this.bus = bus;
    }

    public void Tick()
    {
        if (state.IsDmaInProgress)
        {
            state.DmaTicksElapsed++;

            if (state.DmaTicksElapsed % TicksPerTransfer is 0)
            {
                var nextSourceAddress = state.DmaStartAddress + state.DmaBytesTransferred;
                state.Oam[state.DmaBytesTransferred] = bus.Read((ushort)nextSourceAddress);
                state.DmaBytesTransferred++;
            }

            if (state.DmaTicksElapsed is DmaDurationInTicks)
            {
                state.IsDmaInProgress = false;
                state.DmaTicksElapsed = 0;
            }
        }
    }
}
