using Gameboi.Memory.Io;

namespace Gameboi.Hardware;

internal class IoLogic
{
    private readonly SystemState state;

    public IoLogic(SystemState state) => this.state = state;

    public byte Read(ushort address) => address switch
    {
        // Joypad -------------------------------
        0 => (byte)(state.IoPorts[0] | 0b1100_0000),

        // Interrupts ---------------------------
        IF_index => (byte)(state.IoPorts[IF_index] | 0b1110_0000),

        // Sound --------------------------------
        // Ch1
        NR11_index => (byte)(state.IoPorts[NR11_index] | 0b0001_1111),
        NR13_index => WriteOnly,
        NR14_index => (byte)(state.IoPorts[NR14_index] | 0b1011_1111),
        // Ch2
        NR21_index => (byte)(state.IoPorts[NR21_index] | 0b0001_1111),
        NR23_index => WriteOnly,
        NR24_index => (byte)(state.IoPorts[NR24_index] | 0b1011_1111),
        // Ch3
        NR30_index => (byte)(state.IoPorts[NR30_index] | 0b0111_1111),
        NR31_index => WriteOnly,
        NR32_index => (byte)(state.IoPorts[NR32_index] | 0b1001_1111),
        NR33_index => WriteOnly,
        NR34_index => (byte)(state.IoPorts[NR34_index] | 0b1011_1111),
        // Ch4
        NR41_index => WriteOnly,
        NR44_index => (byte)(state.IoPorts[NR44_index] | 0b1011_1111),
        // Global control
        NR52_index => (byte)(state.IoPorts[NR52_index] | 0b0111_0000),

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
            P1_index => (byte)((value & 0b0011_0000) | (state.IoPorts[P1_index] & 0b0000_1111)),

            // Timer ---------------------------
            // Resets when written to.
            DIV_index => 0,

            // Sound ---------------------------
            NR52_index => (byte)((value & 0b1000_0000) | (state.IoPorts[NR52_index] & 0b0111_1111)),

            // LCD -----------------------------
            // Bits 0-2 are readonly.
            STAT_index => (byte)((value & 0xf8) | (state.IoPorts[address] & 7)),
            // Resets when written to.
            LY_index => 0,
            // Is constantly compared to LY and sets coincidence flag in stat.
            LYC_index => LycWriteLogic(value),

            _ => value
        };
    }

    private byte LycWriteLogic(byte value)
    {
        if (value == state.IoPorts[LY_index])
        {
            // set Stat coincident flag
            Stat Stat = state.IoPorts[STAT_index];
            state.IoPorts[STAT_index] = Stat.WithCoincidenceFlag(true);
        }
        return value;
    }

    private const byte WriteOnly = 0xff;

    private const ushort P1_index = 0;

    private const ushort DIV_index = 4;

    private const ushort IF_index = 0x0f;

    private const ushort NR10_index = 0x10;
    private const ushort NR11_index = 0x11;
    private const ushort NR12_index = 0x12;
    private const ushort NR13_index = 0x13;
    private const ushort NR14_index = 0x14;

    private const ushort NR21_index = 0x16;
    private const ushort NR22_index = 0x17;
    private const ushort NR23_index = 0x18;
    private const ushort NR24_index = 0x19;

    private const ushort NR30_index = 0x1a;
    private const ushort NR31_index = 0x1b;
    private const ushort NR32_index = 0x1c;
    private const ushort NR33_index = 0x1d;
    private const ushort NR34_index = 0x1e;

    private const ushort NR41_index = 0x20;
    private const ushort NR42_index = 0x21;
    private const ushort NR43_index = 0x22;
    private const ushort NR44_index = 0x23;

    private const ushort NR50_index = 0x24;
    private const ushort NR51_index = 0x25;
    private const ushort NR52_index = 0x26;

    private const ushort LCDC_index = 0x40;
    private const ushort STAT_index = 0x41;
    private const ushort SCY_index = 0x42;
    private const ushort SCX_index = 0x43;
    private const ushort LY_index = 0x44;
    private const ushort LYC_index = 0x45;
    private const ushort DMA_index = 0x46;
    private const ushort BGP_index = 0x47;
    private const ushort OBP_0_index = 0x48;
    private const ushort OBP_1_index = 0x49;
    private const ushort WY_index = 0x4a;
    private const ushort WX_index = 0x4b;
}
