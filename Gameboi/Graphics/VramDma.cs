namespace Gameboi.Graphics;

public class VramDma
{
    ushort Source => (ushort)((state.HDMA1 << 8) | state.HDMA2 & 0xF0);
    ushort Destination => (ushort)(((state.HDMA3 | 0x80) << 8) | state.HDMA4 & 0xF0);

    private readonly SystemState state;
    private readonly Bus bus;

    public VramDma(SystemState state, Bus bus)
    {
        this.state = state;
        this.bus = bus;
    }

    public void TransferBlock()
    {
        var offset = state.VramDmaBlocksTransferred * 0x10;
        for (int i = 0; i < 0x10; i++)
        {
            bus.Write((ushort)(Source + offset + i), bus.Read((ushort)(Destination + offset + i)));
        }
        state.VramDmaBlocksTransferred += 1;

        if (state.HDMA5 is 0)
        {
            state.IsVramDmaInProgress = false;
            state.HDMA5 = 0xff;
        }
        else
        {
            state.HDMA5 -= 1;
        }
    }

    public void Tick()
    {
        if (state.VramDmaTicksLeftOfBlockTransfer is 0)
        {
            TransferBlock();
            state.VramDmaTicksLeftOfBlockTransfer = 7;
        }
        else
        {
            state.VramDmaTicksLeftOfBlockTransfer -= 1;
        }
    }
}
