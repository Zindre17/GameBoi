using Gameboi.Extensions;
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
        //----------------------------------------
        2 => (byte)(state.IoPorts[2] | 0b0111_1110),
        3 => Unused,
        DIV_index => state.TimerCounter.GetHighByte(),
        TAC_index => (byte)(state.Tac | 0b1111_1100),
        8 or 9 or 0xa or 0xb or 0xc or 0xd or 0xe => Unused,
        // Interrupts ---------------------------
        IF_index => (byte)(state.InterruptFlags | 0b1110_0000),

        // Sound --------------------------------
        // Ch1
        NR10_index => (byte)(state.NR10 | 0x80),
        NR11_index => (byte)(state.NR11 | 0b0001_1111),
        NR13_index => WriteOnly,
        NR14_index => (byte)(state.NR14 | 0b1011_1111),
        0x15 => Unused,
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
        0x1f => Unused,
        // Ch4
        NR41_index => WriteOnly,
        NR44_index => (byte)(state.NR44 | 0b1011_1111),
        // Global control
        NR52_index => (byte)(state.NR52 | 0b0111_0000),
        0x27 or 0x28 or 0x29 => Unused,

        // LCD ----------------------------------
        STAT_index => (byte)(state.LcdStatus | 0x80),
        0x4c => Unused,
        0x4e => Unused,
        VBK_index => (byte)(state.IoPorts[VBK_index] | 0xfe),
        0x50 => Unused,
        0x57 or 0x58 or 0x59 or 0x5a or 0x5b or 0x5c or 0x5d or 0x5e or 0x5f => Unused,
        0x60 or 0x61 or 0x62 or 0x63 or 0x64 or 0x65 or 0x66 or 0x67 => Unused,
        BCPS_index => (byte)(state.BCPS | 0b0100_0000),
        BCPD_index => state.BackgroundColorPaletteData[state.BCPS & 0x3f],
        OCPS_index => (byte)(state.OCPS | 0b0100_0000),
        OCPD_index => state.ObjectColorPaletteData[state.OCPS & 0x3f],
        0x6d or 0x6e or 0x6f => Unused,
        > 0x70 and < 0xff => Unused,
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
            DIV_index => DivWriteLogic(value),
            TAC_index => TacWriteLogic(value),

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
            HDMA5_index => Hdma5WriteLogic(value),
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

    private byte TacWriteLogic(byte value)
    {
        var oldTac = new Tac(state.Tac);
        var newTac = new Tac(value);

        var wasMultiplexerHigh = ImprovedTimer.IsMultiplexerHigh(oldTac.TimerSpeedSelect, state.TimerCounter);

        if (oldTac.IsTimerEnabled && wasMultiplexerHigh)
        {
            var isMultiplexerLow = ImprovedTimer.IsMultiplexerLow(newTac.TimerSpeedSelect, state.TimerCounter);
            if (!newTac.IsTimerEnabled || isMultiplexerLow)
            {
                ImprovedTimer.IncrementTima(state);
                return value;
            }
        }

        return value;
    }

    private byte DivWriteLogic(byte value)
    {
        var tac = new Tac(state.Tac);
        var IsMultiplexerHigh = ImprovedTimer.IsMultiplexerHigh(tac.TimerSpeedSelect, state.TimerCounter);
        if (IsMultiplexerHigh && tac.IsTimerEnabled)
        {
            ImprovedTimer.IncrementTima(state);
        }
        state.TimerCounter = 0;
        return value;
    }

    private byte Hdma5WriteLogic(byte value)
    {
        if (state.IsVramDmaInProgress)
        {
            state.IsVramDmaInProgress = !value.IsBitSet(7);
            return (byte)(value | 0x80);
        }

        state.IsVramDmaInProgress = true;
        state.VramDmaModeIsHblank = value.IsBitSet(7);
        state.VramDmaBlockCount = state.HDMA5 & 0x7f + 1;
        state.VramDmaBlocksTransferred = 0;

        return (byte)(value & 0x7f);
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
            state.LcdLinesOfWindowDrawnThisFrame = 0;
            state.LcdWindowTriggered = false;
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
    private const byte Unused = 0xff;
}
