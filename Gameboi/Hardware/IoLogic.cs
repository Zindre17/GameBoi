using Gameboi.Extensions;
using Gameboi.Memory.Io;
using Gameboi.Memory.Specials;
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

            SC_index => SerialControlWriteLogic(value),

            // Timer ---------------------------
            // Resets when written to.
            DIV_index => DivWriteLogic(value),
            TAC_index => TacWriteLogic(value),
            TIMA_index => TimaWriteLogic(value),

            // Sound ---------------------------
            NR52_index => (byte)((value & 0b1000_0000) | (state.NR52 & 0b0111_1111)),
            NR12_index => Nr12WriteLogic(value),
            NR14_index => Nr14WriteLogic(value),
            NR22_index => Nr22WriteLogic(value),
            NR24_index => Nr24WriteLogic(value),
            NR30_index => Nr30WriteLogic(value),
            NR34_index => Nr34WriteLogic(value),
            // LCD -----------------------------
            // Bits 0-2 are readonly.
            LCDC_index => LcdControlWriteLogic(value),
            STAT_index => LcdStatusWriteLogic(value),
            // Resets when written to.
            LY_index => 0,
            // Is constantly compared to LY and sets coincidence flag in stat.
            LYC_index => LycWriteLogic(value),
            DMA_index => DmaWriteLogic(value),
            HDMA5_index => Hdma5WriteLogic(value),
            VBK_index => VramBankSelectLogic(value),
            BCPD_index => BcpdWriteLogic(value),
            OCPD_index => OcpdWriteLogic(value),
            SVBK_index => WorkRamBankSelectLogic(value),
            _ => value
        };
    }

    private byte Nr34WriteLogic(byte value)
    {
        if (value.IsBitSet(7))
        {
            state.NR52 = state.NR52.SetBit(2);
            state.Channel3SampleNr = 0;
            state.Channel3SamplesForCurrentWaveSample = 0;
        }
        if (value.IsBitSet(6))
        {
            // DOCs say this uses all 8 bits, but that does not make sense considering we count to 64.
            // TODO: This actually counts upwards to 64, but we count down to 0 so we suptract duration from 64.
            // state.Channel3Duration = 64 - (state.NR31 & 0b0011_1111);
            state.Channel3Duration = 0x100 - state.NR31;
        }
        return value;
    }

    private byte Nr30WriteLogic(byte value)
    {
        if (!value.IsBitSet(7))
        {
            state.NR52 = state.NR52.UnsetBit(2);
        }
        return value;
    }

    private byte Nr24WriteLogic(byte value)
    {
        if (value.IsBitSet(7))
        {
            state.NR52 = state.NR52.SetBit(1);
            state.Channel2Envelope = state.NR22;
        }
        if (value.IsBitSet(6))
        {
            // TODO: This actually counts upwards to 64, but we count down to 0 so we suptract duration from 64.
            state.Channel2Duration = 64 - (state.NR21 & 0b0011_1111);
        }
        return value;
    }

    private byte Nr14WriteLogic(byte value)
    {
        if (value.IsBitSet(7))
        {
            state.NR52 = state.NR52.SetBit(0);
            state.Channel1Envelope = state.NR12;
        }
        if (value.IsBitSet(6))
        {
            // TODO: This actually counts upwards to 64, but we count down to 0 so we suptract duration from 64.
            state.Channel1Duration = 64 - (state.NR11 & 0b0011_1111);
        }
        return value;
    }

    private byte Nr22WriteLogic(byte value)
    {
        if ((value & 0xf8) is 0)
        {
            state.NR52 = state.NR52.UnsetBit(1);
        }
        return value;
    }

    private byte Nr12WriteLogic(byte value)
    {
        if ((value & 0xf8) is 0)
        {
            state.NR52 = state.NR52.UnsetBit(0);
        }
        return value;
    }

    private byte DmaWriteLogic(byte value)
    {
        // OAM DMA start 4 clock later (1 cycle) 
        //but since the cpu has ticked one more than dma at this point we need an extra tick here
        state.TicksUntilDmaStarts = 5; // 4 + 1
        state.DmaTicksElapsed = 0;
        return value;
    }

    private byte SerialControlWriteLogic(byte value)
    {
        if (value.IsBitSet(7) && value.IsBitSet(0))
        {
            state.SerialTransferBitsLeft = 8;
        }
        return value;
    }

    private byte TimaWriteLogic(byte value)
    {
        if (state.TicksUntilTimerInterrupt > 0)
        {
            state.TicksUntilTimerInterrupt = 0;
            return value;
        }
        if (state.TicksLeftOfTimaReload > 0)
        {
            return state.Tima;
        }

        return value;
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

        if (ImprovedTimer.IsSoundClockMultiplexerHigh(state.TimerCounter, state.IsInDoubleSpeedMode))
        {
            state.SoundTicks += 1;
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

        if (!newControl.IsLcdEnabled)
        {
            state.LcdStatus = status.WithMode(0);
            state.LineY = 0;
            state.LcdLinesOfWindowDrawnThisFrame = 0;
            state.LcdWindowTriggered = false;
            state.LcdRemainingTicksInMode = 208;
        }
        else if (control.IsLcdEnabled is false)
        {
            state.LcdStatus = status.WithCoincidenceFlag(state.LineY == state.LineYCompare);
            status = state.LcdStatus;
            if (status.IsCoincidenceInterruptEnabled && status.CoincidenceFlag)
            {
                if (!state.WasPreviousLcdInterruptLineHigh)
                {
                    var interruptFlags = new InterruptState(state.InterruptFlags);
                    state.InterruptFlags = interruptFlags.WithLcdStatusSet();
                }
                state.WasPreviousLcdInterruptLineHigh = true;
            }
        }

        return value;
    }

    private byte LcdStatusWriteLogic(byte value)
    {
        LcdStatus status = state.LcdStatus;
        LcdStatus newStatus = value;

        var interruptFlags = new InterruptState(state.InterruptFlags);

        switch (status.Mode)
        {
            case 0:
                if (newStatus.IsHblankInterruptEnabled && !state.WasPreviousLcdInterruptLineHigh)
                {
                    state.InterruptFlags = interruptFlags.WithLcdStatusSet();
                    state.WasPreviousLcdInterruptLineHigh = true;
                }
                break;
            case 1:
                if (newStatus.IsVblankInterruptEnabled && !state.WasPreviousLcdInterruptLineHigh)
                {
                    state.InterruptFlags = interruptFlags.WithLcdStatusSet();
                    state.WasPreviousLcdInterruptLineHigh = true;
                }
                break;
            case 2:
                if (newStatus.IsOAMInterruptEnabled && !state.WasPreviousLcdInterruptLineHigh)
                {
                    state.InterruptFlags = interruptFlags.WithLcdStatusSet();
                    state.WasPreviousLcdInterruptLineHigh = true;
                }
                break;
        }

        return (byte)((value & 0xf8) | (state.LcdStatus & 7));
    }

    private byte LycWriteLogic(byte value)
    {
        LcdControl control = state.LcdControl;
        if (control.IsLcdEnabled)
        {
            // set Stat coincident flag
            LcdStatus stat = state.LcdStatus;
            state.LcdStatus = stat.WithCoincidenceFlag(value == state.LineY);
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
