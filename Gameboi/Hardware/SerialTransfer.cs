using Gameboi.Memory.Specials;

namespace Gameboi.Hardware;

public class SerialTransfer
{
    public readonly SystemState state;

    public SerialTransfer(SystemState state)
    {
        this.state = state;
    }

    public void Tick()
    {
        if (state.SerialTransferBitsLeft > 0)
        {
            if ((state.TimerCounter & 0x01ff) is 0x01ff)
            {
                state.SerialTransferData <<= 1;
                // TODO: receive bit from external source or default if not connected
                state.SerialTransferData |= 1;

                state.SerialTransferBitsLeft -= 1;

                if (state.SerialTransferBitsLeft is 0)
                {
                    var interruptFlags = new InterruptState(state.InterruptFlags);
                    state.InterruptFlags = interruptFlags.WithSerialPortSet();

                    state.SerialTransferControl &= 0x7f;
                }
            }
        }
    }
}
