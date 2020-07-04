public static class GeneralMemoryMap
{
    //   0000-3FFF   16KB ROM Bank 00     (in cartridge, fixed at bank 00)
    public const ushort ROM_bank_0_StartAddress = 0;
    public const ushort ROM_bank_0_EndAddress = 0x4000;

    //   4000-7FFF   16KB ROM Bank 01..NN (in cartridge, switchable bank number)
    public const ushort ROM_bank_n_StartAddress = ROM_bank_0_EndAddress;
    public const ushort ROM_bank_n_EndAddress = 0x8000;

    //   8000-9FFF   8KB Video RAM (VRAM) (switchable bank 0-1 in CGB Mode)
    public const ushort VRAM_StartAddress = ROM_bank_n_EndAddress;
    public const ushort VRAM_EndAddress = 0xA000;

    //   A000-BFFF   8KB External RAM     (in cartridge, switchable bank, if any)
    public const ushort ExtRAM_StartAddress = VRAM_EndAddress;
    public const ushort ExtRAM_EndAddress = 0xC000;

    //   C000-CFFF   4KB Work RAM Bank 0 (WRAM)
    public const ushort WRAM_0_StartAddress = ExtRAM_EndAddress;
    public const ushort WRAM_0_EndAddress = 0xD000;

    //   D000-DFFF   4KB Work RAM Bank 1 (WRAM)  (switchable bank 1-7 in CGB Mode)
    public const ushort WRAM_1_StartAddress = WRAM_0_EndAddress;
    public const ushort WRAM_1_EndAddress = 0xE000;

    //   E000-FDFF   Same as C000-DDFF (ECHO)    (typically not used)
    public const ushort WRAM_ECHO_StartAddress = WRAM_1_EndAddress;
    public const ushort WRAM_ECHO_EndAddress = 0xFE00;

    //   FE00-FE9F   Sprite Attribute Table (OAM)
    public const ushort OAM_StartAddress = WRAM_ECHO_EndAddress;
    public const ushort OAM_EndAddress = 0xFEA0;

    //   FEA0-FEFF   Not Usable
    public const ushort Unusable_StartAddress = OAM_EndAddress;
    public const ushort Unusable_EndAddress = 0xFF00;

    //   FF00-FF7F   I/O Ports
    public const ushort IO_StartAddress = Unusable_EndAddress;
    public const ushort IO_EndAddress = 0xFF80;

    //   FF80-FFFE   High RAM (HRAM)
    public const ushort HRAM_StartAddress = IO_EndAddress;
    public const ushort HRAM_EndAddress = 0xFFFF;

    //   FFFF        Interrupt Enable Register
    public const ushort IE_address = 0xFFFF;
}

public static class TimerAddresses
{

    public const ushort DIV_address = 0xFF04; // Divider register
    public const ushort TIMA_address = 0xFF05; // Timer counter
    public const ushort TMA_address = 0xFF06; // Timer modulo
    public const ushort TAC_address = 0xFF07; // Timer control
}

public static class MiscSpecialAddresses
{
    public const ushort DMA_address = 0xFF46;
}

public static class InterruptAddresses
{
    public const ushort IE_address = 0xFFFF;
    public const ushort IF_address = 0xFF0F;

    public const ushort VblankVector = 0x0040;
    public const ushort LcdStatVector = 0x0048;
    public const ushort TimerVector = 0x0050;
    public const ushort SerialVector = 0x0058;
    public const ushort JoypadVector = 0x0060;
}

public static class Frequencies
{
    // TAC speeds
    // bit 2 : 0 = Stop, 1 = Start
    // bit 1 - 0: 
    //      00 = 4096Hz = 0x1000Hz,
    //      01 = 262144Hz = 0x40000Hz, 
    //      10 = 65536Hz = 0x10000Hz, 
    //      11 = 16384Hz = 0x4000Hz  
    public static readonly uint[] timerSpeeds = new uint[4]{
        0x1000,
        0x40000,
        0x10000,
        0x4000
    };

    public static readonly uint cpuSpeed = 0x400000;

    public static readonly uint[] cpuToTimerRatio = new uint[4]{
        cpuSpeed / timerSpeeds[0],
        cpuSpeed / timerSpeeds[1],
        cpuSpeed / timerSpeeds[2],
        cpuSpeed / timerSpeeds[3]
    };

    // DIV is incremented at 16384Hz = 0x4000Hz
    public static readonly uint cpuToDivRatio = cpuToTimerRatio[3];
}