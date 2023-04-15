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
        VBK_index => (byte)(state.IoPorts[VBK_index] | 0xfe),
        BCPS_index => (byte)(state.BCPS | 0b0100_0000),
        BCPD_index => state.BackgroundColorPaletteData[state.BCPS & 0x3f],
        OCPS_index => (byte)(state.OCPS | 0b0100_0000),
        OCPD_index => state.ObjectColorPaletteData[state.OCPS & 0x3f],

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
            LCDC_index => LcdControlWriteLogic(value),
            STAT_index => (byte)((value & 0xf8) | (state.LcdStatus & 7)),
            // Resets when written to.
            LY_index => 0,
            // Is constantly compared to LY and sets coincidence flag in stat.
            LYC_index => LycWriteLogic(value),
            VBK_index => VramBankSelectLogic(value),
            BCPD_index => BcpdWriteLogic(value),
            OCPD_index => OcpdWriteLogic(value),
            SVBK_index => WorkRamBankSelectLogic(value),
            _ => value
        };

        if (address is DMA_index)
        {
            state.IsDmaInProgress = true;
            state.DmaStartAddress = (ushort)(state.Dma << 8);
        }
    }

    private byte VramBankSelectLogic(byte value)
    {
        state.VideoRamOffset = 0x2000 * (value & 1);
        return value;
    }

    private byte WorkRamBankSelectLogic(byte value)
    {
        state.WorkRamOffset = 0x1000 * (value & 7);
        if (state.WorkRamOffset is 0)
        {
            state.WorkRamOffset = 0x1000;
        }
        return value;
    }

    private byte BcpdWriteLogic(byte value)
    {
        var index = state.BCPS & 0x3f;
        state.BackgroundColorPaletteData[index] = value;
        if ((state.BCPS & 0x80) is 0x80)
        {
            index += 1;
            index &= 0x3f; // wraparound (0x3f + 1 -> 0)
            index |= 0x80; // enable autoincrement;
            state.BCPS = (byte)index;
        }
        return value;
    }

    private byte OcpdWriteLogic(byte value)
    {
        var index = state.OCPS & 0x3f;
        state.ObjectColorPaletteData[index] = value;
        if ((state.OCPS & 0x80) is 0x80)
        {
            index += 1;
            index &= 0x3f; // wraparound (0x3f + 1 -> 0)
            index |= 0x80; // enable autoincrement;
            state.OCPS = (byte)index;
        }
        return value;
    }

    private byte LcdControlWriteLogic(byte value)
    {
        LcdControl control = state.LcdControl;
        LcdControl newControl = value;
        LcdStatus status = state.LcdStatus;

        // disabled => enabled
        if (newControl.IsLcdEnabled && control.IsLcdEnabled is false)
        {
            state.LineY = 0;
            state.LcdRemainingTicksInMode = 80;
            state.LcdStatus = status.WithMode(2);
        }

        return value;
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
