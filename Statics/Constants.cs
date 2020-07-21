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

public static class ScreenRelatedAddresses
{
    public const ushort LCDC_address = 0xFF40;
    public const ushort STAT_address = 0xFF41;
    public const ushort SCY_address = 0xFF42;
    public const ushort SCX_address = 0xFF43;
    public const ushort LY_address = 0xFF44;
    public const ushort LYC_address = 0xFF45;
    public const ushort BGP_address = 0xFF47;
    public const ushort OBP0_address = 0xFF48;
    public const ushort OBP1_address = 0xFF49;
    public const ushort WY_address = 0xFF4A;
    public const ushort WX_address = 0xFF4B;

    public const ushort TileDataStart = GeneralMemoryMap.VRAM_StartAddress;
    public const ushort TileDataEnd = 0x9800;
    public const ushort TileData1Start = TileDataStart;
    public const ushort TileData1End = 0x9000;
    public const ushort TileData0Start = 0x8800;
    public const ushort TileData0End = TileDataEnd;

    public const ushort TileMapStart = TileData0End;
    public const ushort TileMapEnd = GeneralMemoryMap.VRAM_EndAddress;
    public const ushort TileMap0Start = TileMapStart;
    public const ushort TileMap0End = 0x9C00;
    public const ushort TileMap1Start = TileMap0End;
    public const ushort TileMap1End = TileMapEnd;
}

public static class ScreenTimings
{
    public const uint clocksPerDraw = 70224;
    public const ushort vblankClocks = 4560;
    public const byte mode2Clocks = 80;
    public const byte mode3Clocks = 172;
    public const ushort hblankClocks = 204;
}

public static class ScreenSizes
{
    public const int pixelsPerLine = 160;
    public const int pixelLines = 144;
}

public static class TileDataConstants
{
    public const int tileCount = 0x180;
    public const int startIndexOfSecondTable = 0x100;
    public const int bytesPerTile = 0x10;
    public const int tileDataSize = 0x1800;
}

public static class TileMapConstants
{
    public const int tileMapTotalSize = 0x800;
    public const int tileMapWidth = 32;
    public const int tileMapHeight = 32;
    public const int tileMapSize = 0x400;

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

public static class WavSettings
{
    public const string FILE_TYPE_ID = "RIFF";
    public const uint FILE_LENGTH = 4 + 8 + FORMAT_SIZE + 8 + DATA_SIZE;
    public const string MEDIA_TYPE_ID = "WAVE";

    public const string FORMAT_ID = "fmt ";
    public const uint FORMAT_SIZE = 16;
    public const ushort FORMAT_TAG = 1;
    public const ushort CHANNELS = 1;
    public const uint SAMPLE_RATE = 44100;
    public const uint AVG_BYTES_PER_SEC = (uint)(SAMPLE_RATE * 0.05 * BLOCK_ALIGN);
    public const ushort BLOCK_ALIGN = CHANNELS * (BITS_PER_SAMPLE / 8);
    public const ushort BITS_PER_SAMPLE = 16;

    public const string DATA_ID = "data";
    public const uint DATA_SIZE = (uint)(SAMPLE_RATE * BLOCK_ALIGN); // 100ms

    public const long DATA_SAMPLE_START_INDEX = FILE_LENGTH + 8 - DATA_SIZE;
}

public static class SoundRegisters
{
    public static readonly Address NR10_address = 0xFF10;
    public static readonly Address NR11_address = 0xFF11;
    public static readonly Address NR12_address = 0xFF12;
    public static readonly Address NR13_address = 0xFF13;
    public static readonly Address NR14_address = 0xFF14;
    public static readonly Address NR21_address = 0xFF16;
    public static readonly Address NR22_address = 0xFF17;
    public static readonly Address NR23_address = 0xFF18;
    public static readonly Address NR24_address = 0xFF19;
    public static readonly Address NR30_address = 0xFF1A;
    public static readonly Address NR31_address = 0xFF1B;
    public static readonly Address NR32_address = 0xFF1C;
    public static readonly Address NR33_address = 0xFF1D;
    public static readonly Address NR34_address = 0xFF1E;
    public static readonly Address NR41_address = 0xFF20;
    public static readonly Address NR42_address = 0xFF21;
    public static readonly Address NR43_address = 0xFF22;
    public static readonly Address NR44_address = 0xFF23;
    public static readonly Address NR50_address = 0xFF24;
    public static readonly Address NR51_address = 0xFF25;
    public static readonly Address NR52_address = 0xFF26;

}