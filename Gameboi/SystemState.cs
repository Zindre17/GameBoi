using System;
using System.Collections.Generic;
using Gameboi.Extensions;
using Gameboi.Statics;
using static Gameboi.IoIndices;

namespace Gameboi;

public class SystemState
{
    // Cartridge memory
    public byte[] CartridgeRom { get; private set; } = Array.Empty<byte>();
    public byte[] CartridgeRam { get; private set; } = Array.Empty<byte>();

    // MBC state
    public bool MbcRamDisabled { get; set; } = true;
    public int MbcRamOffset { get; set; } = 0;

    public int MbcRom0Offset { get; set; } = 0;
    public int MbcRom1Offset { get; set; } = MbcCartridgeBankSizes.RomBankSize;

    public int MbcRomSelectLow { get; set; } = 0;
    public int MbcRomSelectHigh { get; set; } = 0;

    public int MbcRamSelect { get; set; } = 0;

    public int MbcMode { get; set; } = 0;

    // Gameboy state (memory and IO)
    public byte[] VideoRam { get; private set; } = new byte[0x4_000]; // Color specs
    public int VideoRamOffset = 0;
    public byte[] WorkRam { get; private set; } = new byte[0x8_000]; // Color specs
    public int WorkRamOffset = 0x1000;
    public byte[] Oam { get; private set; } = new byte[0xA0];
    public byte[] IoPorts { get; private set; } = new byte[0x80];
    public byte[] HighRam { get; private set; } = new byte[0x7F];

    private byte interruptEnableRegister;
    public ref byte InterruptEnableRegister => ref interruptEnableRegister;

    public int TicksLeftOfInstruction { get; set; } = 0;

    // Cpu registers
    private byte accumulator;
    public ref byte Accumulator => ref accumulator;
    public byte flags;
    public ref byte Flags => ref flags;

    public byte b;
    public ref byte B => ref b;
    public byte c;
    public ref byte C => ref c;

    public ushort BC => B.Concat(C);

    public byte d;
    public ref byte D => ref d;
    public byte e;
    public ref byte E => ref e;

    public ushort DE => D.Concat(E);

    public byte high;
    public ref byte High => ref high;
    public byte low;
    public ref byte Low => ref low;

    public ushort HL => high.Concat(low);

    public ushort ProgramCounter { get; set; }

    public ushort stackPointer;
    public ref ushort StackPointer => ref stackPointer;

    // etc
    public bool InterruptMasterEnable { get; set; } = true;
    public bool IsInterruptMasterEnablePreparing { get; set; } = false;
    public bool IsHalted { get; set; } = false;
    public bool IsInDoubleSpeedMode => SpeedControl.IsBitSet(7);

    public int LcdRemainingTicksInMode { get; set; } = ScreenTimings.mode2Clocks;
    public int LcdLinesOfWindowDrawnThisFrame { get; set; } = 0;
    public bool LcdWindowTriggered { get; set; } = false;

    public bool IsInCbMode { get; set; } = false;

    public int TicksUntilDmaStarts { get; set; } = 0;
    public bool IsDmaInProgress { get; set; } = false;
    public int DmaTicksElapsed { get; set; } = 0;

    public bool IsVramDmaInProgress { get; set; } = false;
    public bool VramDmaModeIsHblank { get; set; } = false;
    public int VramDmaBlockCount { get; set; } = 0;
    public int VramDmaBlocksTransferred { get; set; } = 0;
    public int VramDmaTicksLeftOfBlockTransfer { get; set; } = 0;

    public ushort TimerCounter { get; set; } = 0;
    public int TicksUntilTimerInterrupt { get; set; } = -1;
    public int TicksLeftOfTimaReload { get; set; } = -1;

    public int SerialTransferBitsLeft { get; set; } = 0;

    public byte[] BackgroundColorPaletteData = new byte[64];
    public byte[] ObjectColorPaletteData = new byte[64];

    public void ChangeGame(byte[] cartridgeRom, byte[] cartridgeRam)
    {
        CartridgeRom = cartridgeRom;
        CartridgeRam = cartridgeRam;

        Reset();
    }

    public void Reset()
    {
        ResetCpu();
        ResetCartridge();
        ResetGameboiState();
    }

    private void ResetCpu()
    {
        Accumulator = 0x11;
        ProgramCounter = 0x100;
        StackPointer = 0xFFFE;
        Flags = 0x80;
        B = 0;
        C = 0;
        D = 0;
        E = 0x08;
        High = 0;
        Low = 0x7c;
    }

    private void ResetCartridge()
    {
        CartridgeRam.Clear();
        MbcMode = 0;
        MbcRamDisabled = true;
        MbcRamOffset = 0;
        MbcRom0Offset = 0;
        MbcRom1Offset = MbcCartridgeBankSizes.RomBankSize;
        MbcRomSelectHigh = 0;
        MbcRomSelectLow = 0;
    }

    private void ResetGameboiState()
    {
        VideoRam.Clear();
        VideoRamOffset = 0;
        WorkRam.Clear();
        WorkRamOffset = 0x1000;
        HighRam.Clear();
        Oam.Clear();
        ResetIO();

        TicksLeftOfInstruction = 0;

        InterruptEnableRegister = 0;

        InterruptMasterEnable = true;
        IsInterruptMasterEnablePreparing = false;

        IsHalted = false;

        IsInCbMode = false;

        TicksUntilDmaStarts = 0;
        IsDmaInProgress = false;
        DmaTicksElapsed = 0;

        IsVramDmaInProgress = false;
        VramDmaModeIsHblank = false;
        VramDmaBlockCount = 0;
        VramDmaBlocksTransferred = 0;
        VramDmaTicksLeftOfBlockTransfer = 0;

        TimerCounter = 0;
        TicksUntilTimerInterrupt = 0;
        TicksLeftOfTimaReload = 0;

        SerialTransferBitsLeft = 0;
    }

    private void ResetIO()
    {
        IoPorts.Clear();

        NR10 = 0x80;
        NR11 = 0xbf;
        NR12 = 0xf3;
        NR14 = 0xbf;

        NR21 = 0x3f;
        NR24 = 0xbf;

        NR30 = 0x7f;
        NR31 = 0xff;
        NR32 = 0x9f;
        NR34 = 0xbf;

        NR41 = 0xff;
        NR44 = 0xbf;

        NR50 = 0x77;
        NR51 = 0xf3;
        NR52 = 0xf1;

        LcdControl = 0x91;
        LcdStatus = 0x80;
        LcdRemainingTicksInMode = ScreenTimings.mode2Clocks;
        LcdLinesOfWindowDrawnThisFrame = 0;
        LcdWindowTriggered = false;

        BackgroundPalette = 0xfc;
        ObjectPalette0 = 0xff;
        ObjectPalette1 = 0xff;
    }

    public byte[] ToArray()
    {
        var bytes = new List<byte>();

        // Cartridge state
        bytes.AddRange(BitConverter.GetBytes(CartridgeRam.Length));
        bytes.AddRange(CartridgeRam);
        bytes.AddRange(BitConverter.GetBytes(MbcMode));
        bytes.Add((byte)(MbcRamDisabled ? 1 : 0));
        bytes.AddRange(BitConverter.GetBytes(MbcRamOffset));
        bytes.AddRange(BitConverter.GetBytes(MbcRom0Offset));
        bytes.AddRange(BitConverter.GetBytes(MbcRom1Offset));
        bytes.AddRange(BitConverter.GetBytes(MbcRomSelectHigh));
        bytes.AddRange(BitConverter.GetBytes(MbcRomSelectLow));

        // Gameboy mem/regs
        bytes.AddRange(BitConverter.GetBytes(VideoRam.Length));
        bytes.AddRange(VideoRam);
        bytes.AddRange(BitConverter.GetBytes(WorkRam.Length));
        bytes.AddRange(WorkRam);
        bytes.AddRange(BitConverter.GetBytes(Oam.Length));
        bytes.AddRange(Oam);
        bytes.AddRange(BitConverter.GetBytes(IoPorts.Length));
        bytes.AddRange(IoPorts);
        bytes.AddRange(BitConverter.GetBytes(HighRam.Length));
        bytes.AddRange(HighRam);
        bytes.Add(interruptEnableRegister);

        // Cpu regs and state
        bytes.Add(accumulator);
        bytes.Add(flags);
        bytes.Add(b);
        bytes.Add(c);
        bytes.Add(d);
        bytes.Add(e);
        bytes.Add(high);
        bytes.Add(low);
        bytes.AddRange(BitConverter.GetBytes(stackPointer));
        bytes.AddRange(BitConverter.GetBytes(ProgramCounter));

        bytes.AddRange(BitConverter.GetBytes(TicksLeftOfInstruction));

        bytes.Add((byte)(InterruptMasterEnable ? 1 : 0));
        bytes.Add((byte)(IsInterruptMasterEnablePreparing ? 1 : 0));
        bytes.Add((byte)(IsHalted ? 1 : 0));
        bytes.Add((byte)(IsInCbMode ? 1 : 0));
        bytes.Add((byte)(IsInDoubleSpeedMode ? 1 : 0));

        // Misc IO state
        bytes.AddRange(BitConverter.GetBytes(TicksUntilDmaStarts));
        bytes.Add((byte)(IsDmaInProgress ? 1 : 0));
        bytes.AddRange(BitConverter.GetBytes(DmaTicksElapsed));

        bytes.Add((byte)(IsVramDmaInProgress ? 1 : 0));
        bytes.Add((byte)(VramDmaModeIsHblank ? 1 : 0));
        bytes.AddRange(BitConverter.GetBytes(VramDmaBlockCount));
        bytes.AddRange(BitConverter.GetBytes(VramDmaBlocksTransferred));
        bytes.AddRange(BitConverter.GetBytes(VramDmaTicksLeftOfBlockTransfer));

        bytes.AddRange(BitConverter.GetBytes(TimerCounter));
        bytes.AddRange(BitConverter.GetBytes(TicksUntilTimerInterrupt));
        bytes.AddRange(BitConverter.GetBytes(TicksLeftOfTimaReload));

        bytes.AddRange(BitConverter.GetBytes(LcdRemainingTicksInMode));
        bytes.AddRange(BitConverter.GetBytes(LcdLinesOfWindowDrawnThisFrame));
        bytes.Add((byte)(LcdWindowTriggered ? 1 : 0));

        bytes.AddRange(BitConverter.GetBytes(SerialTransferBitsLeft));

        return bytes.ToArray();
    }

    public void LoadState(byte[] state)
    {
        var index = 0;
        // Cartridge state
        ReadByteArray(CartridgeRam);
        MbcMode = ReadInt();
        MbcRamDisabled = ReadBool();
        MbcRamOffset = ReadInt();
        MbcRom0Offset = ReadInt();
        MbcRom1Offset = ReadInt();
        MbcRomSelectHigh = ReadInt();
        MbcRomSelectLow = ReadInt();

        // Gameboy mem/regs
        ReadByteArray(VideoRam);
        ReadByteArray(WorkRam);
        ReadByteArray(Oam);
        ReadByteArray(IoPorts);
        ReadByteArray(HighRam);
        interruptEnableRegister = ReadByte();

        // Cpu regs and state
        accumulator = ReadByte();
        flags = ReadByte();
        b = ReadByte();
        c = ReadByte();
        d = ReadByte();
        e = ReadByte();
        high = ReadByte();
        low = ReadByte();
        stackPointer = ReadUshort();
        ProgramCounter = ReadUshort();

        TicksLeftOfInstruction = ReadInt();

        InterruptMasterEnable = ReadBool();
        IsInterruptMasterEnablePreparing = ReadBool();
        IsHalted = ReadBool();
        IsInCbMode = ReadBool();

        // Misc IO state
        TicksUntilDmaStarts = ReadInt();
        IsDmaInProgress = ReadBool();
        DmaTicksElapsed = ReadInt();

        IsVramDmaInProgress = ReadBool();
        VramDmaModeIsHblank = ReadBool();
        VramDmaBlockCount = ReadInt();
        VramDmaBlocksTransferred = ReadInt();
        VramDmaTicksLeftOfBlockTransfer = ReadInt();

        TimerCounter = ReadUshort();
        TicksUntilTimerInterrupt = ReadInt();
        TicksLeftOfTimaReload = ReadInt();

        LcdRemainingTicksInMode = ReadInt();
        LcdLinesOfWindowDrawnThisFrame = ReadInt();
        LcdWindowTriggered = ReadBool();

        SerialTransferBitsLeft = ReadInt();

        ushort ReadUshort()
        {
            var result = BitConverter.ToUInt16(state, index);
            index += sizeof(ushort);
            return result;
        }

        byte ReadByte()
        {
            return state[index++];
        }

        void ReadByteArray(byte[] destination)
        {
            var length = ReadInt();
            Array.Copy(state, index, destination, 0, length);
            index += length;
        }

        bool ReadBool()
        {
            return state[index++] is 1;
        }

        int ReadInt()
        {
            var result = BitConverter.ToInt32(state, index);
            index += sizeof(int);
            return result;
        }
    }

    public ref byte P1 => ref IoPorts[P1_index];
    public ref byte SerialTransferData => ref IoPorts[SB_index];
    public ref byte SerialTransferControl => ref IoPorts[SC_index];

    public byte Div => TimerCounter.GetHighByte();
    public ref byte Tima => ref IoPorts[TIMA_index];
    public ref byte Tma => ref IoPorts[TMA_index];
    public ref byte Tac => ref IoPorts[TAC_index];

    public ref byte InterruptFlags => ref IoPorts[IF_index];

    public ref byte NR10 => ref IoPorts[NR10_index];
    public ref byte NR11 => ref IoPorts[NR11_index];
    public ref byte NR12 => ref IoPorts[NR12_index];
    public ref byte NR13 => ref IoPorts[NR13_index];
    public ref byte NR14 => ref IoPorts[NR14_index];

    public ref byte NR21 => ref IoPorts[NR21_index];
    public ref byte NR22 => ref IoPorts[NR22_index];
    public ref byte NR23 => ref IoPorts[NR23_index];
    public ref byte NR24 => ref IoPorts[NR24_index];

    public ref byte NR30 => ref IoPorts[NR30_index];
    public ref byte NR31 => ref IoPorts[NR31_index];
    public ref byte NR32 => ref IoPorts[NR32_index];
    public ref byte NR33 => ref IoPorts[NR33_index];
    public ref byte NR34 => ref IoPorts[NR34_index];

    public ref byte NR41 => ref IoPorts[NR41_index];
    public ref byte NR42 => ref IoPorts[NR42_index];
    public ref byte NR43 => ref IoPorts[NR43_index];
    public ref byte NR44 => ref IoPorts[NR44_index];

    public ref byte NR50 => ref IoPorts[NR50_index];
    public ref byte NR51 => ref IoPorts[NR51_index];
    public ref byte NR52 => ref IoPorts[NR52_index];

    public ref byte LcdControl => ref IoPorts[LCDC_index];
    public ref byte LcdStatus => ref IoPorts[STAT_index];
    public ref byte ScrollY => ref IoPorts[SCY_index];
    public ref byte ScrollX => ref IoPorts[SCX_index];
    public ref byte LineY => ref IoPorts[LY_index];
    public ref byte LineYCompare => ref IoPorts[LYC_index];
    public ref byte Dma => ref IoPorts[DMA_index];
    public ref byte BackgroundPalette => ref IoPorts[BGP_index];
    public ref byte ObjectPalette0 => ref IoPorts[OBP_0_index];
    public ref byte ObjectPalette1 => ref IoPorts[OBP_1_index];
    public ref byte SpeedControl => ref IoPorts[KEY1_index];
    public ref byte HDMA1 => ref IoPorts[HDMA1_index];
    public ref byte HDMA2 => ref IoPorts[HDMA2_index];
    public ref byte HDMA3 => ref IoPorts[HDMA3_index];
    public ref byte HDMA4 => ref IoPorts[HDMA4_index];
    public ref byte HDMA5 => ref IoPorts[HDMA5_index];
    public ref byte BCPS => ref IoPorts[BCPS_index];
    public ref byte BCPD => ref IoPorts[BCPD_index];
    public ref byte OCPS => ref IoPorts[OCPS_index];
    public ref byte OCPD => ref IoPorts[OCPD_index];
    public ref byte WindowY => ref IoPorts[WY_index];
    public ref byte WindowX => ref IoPorts[WX_index];
}

public static class MbcCartridgeBankSizes
{
    public const int RomBankSize = 0x4_000;
    public const int RamBankSize = 0x2_000;
}

public static class IoIndices
{

    public const ushort P1_index = 0x00;

    public const ushort SB_index = 0x01;
    public const ushort SC_index = 0x02;

    public const ushort DIV_index = 0x04;
    public const ushort TIMA_index = 0x05;
    public const ushort TMA_index = 0x06;
    public const ushort TAC_index = 0x07;

    public const ushort IF_index = 0x0f;

    public const ushort NR10_index = 0x10;
    public const ushort NR11_index = 0x11;
    public const ushort NR12_index = 0x12;
    public const ushort NR13_index = 0x13;
    public const ushort NR14_index = 0x14;

    public const ushort NR21_index = 0x16;
    public const ushort NR22_index = 0x17;
    public const ushort NR23_index = 0x18;
    public const ushort NR24_index = 0x19;

    public const ushort NR30_index = 0x1a;
    public const ushort NR31_index = 0x1b;
    public const ushort NR32_index = 0x1c;
    public const ushort NR33_index = 0x1d;
    public const ushort NR34_index = 0x1e;

    public const ushort NR41_index = 0x20;
    public const ushort NR42_index = 0x21;
    public const ushort NR43_index = 0x22;
    public const ushort NR44_index = 0x23;

    public const ushort NR50_index = 0x24;
    public const ushort NR51_index = 0x25;
    public const ushort NR52_index = 0x26;

    public const ushort LCDC_index = 0x40;
    public const ushort STAT_index = 0x41;
    public const ushort SCY_index = 0x42;
    public const ushort SCX_index = 0x43;
    public const ushort LY_index = 0x44;
    public const ushort LYC_index = 0x45;
    public const ushort DMA_index = 0x46;
    public const ushort BGP_index = 0x47;
    public const ushort OBP_0_index = 0x48;
    public const ushort OBP_1_index = 0x49;
    public const ushort KEY1_index = 0x4d;
    public const ushort VBK_index = 0x4f;
    public const ushort HDMA1_index = 0x51;
    public const ushort HDMA2_index = 0x52;
    public const ushort HDMA3_index = 0x53;
    public const ushort HDMA4_index = 0x54;
    public const ushort HDMA5_index = 0x55;

    public const ushort BCPS_index = 0x68;
    public const ushort BCPD_index = 0x69;
    public const ushort OCPS_index = 0x6a;
    public const ushort OCPD_index = 0x6b;
    public const ushort SVBK_index = 0x70;

    public const ushort WY_index = 0x4a;
    public const ushort WX_index = 0x4b;
}
