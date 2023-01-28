using Gameboi.Extensions;

namespace Gameboi;

public class SystemState
{
    // Cartridge memory
    public byte[] CartridgeRom { get; private set; }
    public byte[] CartridgeRam { get; private set; }

    // Gameboy state (memory and IO)
    public byte[] VideoRam { get; private set; }
    public byte[] WorkRam { get; private set; }
    public byte[] Oam { get; private set; } = new byte[0xA0];
    public byte[] IoPorts { get; private set; } = new byte[0x80];
    public byte[] HighRam { get; private set; } = new byte[0x7F];
    public byte InterruptEnableRegister { get; set; }

    // Cpu registers
    public byte Accumulator { get; set; }
    public byte Flags { get; set; }
    public byte B { get; set; }
    public byte C { get; set; }
    public byte D { get; set; }
    public byte E { get; set; }
    public byte High { get; set; }
    public byte Low { get; set; }

    public ushort ProgramCounter { get; set; }
    public ushort StackPointer { get; set; }

    public SystemState(bool color, byte[] cartridgeRom, byte[] cartridgeRam)
    {
        CartridgeRom = cartridgeRom;
        CartridgeRam = cartridgeRam;

        VideoRam = new byte[color ? 0x16_000 : 0x8_000];
        WorkRam = new byte[color ? 0x32_000 : 0x8_000];
    }

    public void Reset(bool color)
    {
        ResetCpu(color);
        ResetCartridge();
        ResetGameboiState();
    }

    private void ResetCpu(bool color)
    {
        Accumulator = (byte)(color ? 0x11 : 0x01);
        ProgramCounter = 0x100;
        StackPointer = 0xFFFE;
        Flags = 0xB0;
        B = 0;
        C = 0x13;
        D = 0;
        E = 0xD8;
        High = 1;
        Low = 0x4D;
    }

    private void ResetCartridge()
    {
        CartridgeRam.Clear();
    }

    private void ResetGameboiState()
    {
        VideoRam.Clear();
        WorkRam.Clear();
        HighRam.Clear();
        Oam.Clear();
        ResetIO();
        InterruptEnableRegister = 0;
    }

    private void ResetIO()
    {
        IoPorts.Clear();

        IoPorts[NR10_index] = 0x80;
        IoPorts[NR11_index] = 0xbf;
        IoPorts[NR12_index] = 0xf3;
        IoPorts[NR14_index] = 0xbf;

        IoPorts[NR21_index] = 0x3f;
        IoPorts[NR24_index] = 0xbf;

        IoPorts[NR30_index] = 0x7f;
        IoPorts[NR31_index] = 0xff;
        IoPorts[NR32_index] = 0x9f;
        IoPorts[NR34_index] = 0xbf;

        IoPorts[NR40_index] = 0xff;
        IoPorts[NR43_index] = 0xbf;

        IoPorts[NR50_index] = 0x77;
        IoPorts[NR51_index] = 0xf3;
        IoPorts[NR52_index] = 0xf1;

        IoPorts[LCDC_index] = 0x91;
        IoPorts[STAT_index] = 0x80;

        IoPorts[BGP_index] = 0xfc;
        IoPorts[OBP_0_index] = 0xff;
        IoPorts[OBP_1_index] = 0xff;
    }

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
    private const ushort NR34_index = 0x1e;

    private const ushort NR40_index = 0x20;
    private const ushort NR41_index = 0x21;
    private const ushort NR42_index = 0x22;
    private const ushort NR43_index = 0x23;

    private const ushort NR50_index = 0x24;
    private const ushort NR51_index = 0x25;
    private const ushort NR52_index = 0x26;

    private const ushort LCDC_index = 0x40;
    private const ushort STAT_index = 0x41;
    private const ushort SCY_index = 0x42;
    private const ushort SCX_index = 0x43;
    private const ushort LY_index = 0x44;
    private const ushort LYC_index = 0x45;
    private const ushort BGP_index = 0x47;
    private const ushort OBP_0_index = 0x48;
    private const ushort OBP_1_index = 0x49;
    private const ushort WY_index = 0x4a;
    private const ushort WX_index = 0x4b;
}
