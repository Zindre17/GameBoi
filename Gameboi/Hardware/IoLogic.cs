using Gameboi.Memory.Io;
using static Gameboi.IoIndices;

namespace Gameboi.Hardware;

internal class IoLogic
{
    private readonly SystemState state;

    public IoLogic(SystemState state) => this.state = state;

    public byte Read(ushort address) => address switch
    {
        // Joypad -------------------------------
        P1_index => (byte)(state.P1 | 0b1100_0000),

        // Interrupts ---------------------------
        IF_index => (byte)(state.InterruptFlags | 0b1110_0000),

        // Sound --------------------------------
        // Ch1
        NR11_index => (byte)(state.NR11 | 0b0001_1111),
        NR13_index => WriteOnly,
        NR14_index => (byte)(state.NR14 | 0b1011_1111),
        // Ch2
        NR21_index => (byte)(state.NR21 | 0b0001_1111),
        NR23_index => WriteOnly,
        NR24_index => (byte)(state.NR24 | 0b1011_1111),
        // Ch3
        NR30_index => (byte)(state.NR30 | 0b0111_1111),
        NR31_index => WriteOnly,
        NR32_index => (byte)(state.NR32 | 0b1001_1111),
        NR33_index => WriteOnly,
        NR34_index => (byte)(state.NR34 | 0b1011_1111),
        // Ch4
        NR41_index => WriteOnly,
        NR44_index => (byte)(state.NR44 | 0b1011_1111),
        // Global control
        NR52_index => (byte)(state.NR52 | 0b0111_0000),

        // LCD ----------------------------------
        DMA_index => WriteOnly,

        _ => state.IoPorts[address]
    };

    public void Write(ushort address, byte value)
    {
        state.IoPorts[address] = address switch
        {
            // Joypad --------------------------
            // Bits 0-3 are readonly, and 6-7 is don't care.
            P1_index => (byte)((value & 0b0011_0000) | (state.P1 & 0b0000_1111)),

            // Timer ---------------------------
            // Resets when written to.
            DIV_index => 0,

            // Sound ---------------------------
            NR52_index => (byte)((value & 0b1000_0000) | (state.NR52 & 0b0111_1111)),

            // LCD -----------------------------
            // Bits 0-2 are readonly.
            STAT_index => (byte)((value & 0xf8) | (state.LcdStatus & 7)),
            // Resets when written to.
            LY_index => 0,
            // Is constantly compared to LY and sets coincidence flag in stat.
            LYC_index => LycWriteLogic(value),

            _ => value
        };
    }

    private byte LycWriteLogic(byte value)
    {
        if (value == state.LineY)
        {
            // set Stat coincident flag
            LcdStatus stat = state.LcdStatus;
            state.LcdStatus = stat.WithCoincidenceFlag(true);
        }
        return value;
    }

    internal ref byte GetRef(ushort address)
    {
        return ref state.IoPorts[address];
    }

    private const byte WriteOnly = 0xff;
}
