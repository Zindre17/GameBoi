namespace Gameboi.Hardware;

public class Dma
{
    private readonly SystemState state;
    private readonly Bus bus;

    private const byte ByteTransferCount = 0xa0;
    private const byte TicksPerTransfer = 4;
    private const int DmaDurationInTicks = ByteTransferCount * TicksPerTransfer;

    public Dma(SystemState state, Bus bus)
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
                var currentByte = state.DmaTicksElapsed / TicksPerTransfer - 1;
                var nextSourceAddress = (state.Dma << 8) + currentByte;
                state.Oam[currentByte] = bus.Read((ushort)nextSourceAddress);
            }

            if (state.DmaTicksElapsed is DmaDurationInTicks)
            {
                state.IsDmaInProgress = false;
                state.DmaTicksElapsed = 0;
            }
        }

        if (state.TicksUntilDmaStarts > 0)
        {
            state.TicksUntilDmaStarts -= 1;
            if (state.TicksUntilDmaStarts is 0)
            {
                state.IsDmaInProgress = true;
                state.DmaTicksElapsed = 0;
            }
        }
    }
}
