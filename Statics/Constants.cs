public static class MemoryAddresses
{
    //   0000-3FFF   16KB ROM Bank 00     (in cartridge, fixed at bank 00)
    public const ushort ROM_bank_0_StartAddress = 0;

    //   4000-7FFF   16KB ROM Bank 01..NN (in cartridge, switchable bank number)
    public const ushort ROM_bank_n_StartAddress = 0x4000;

    //   8000-9FFF   8KB Video RAM (VRAM) (switchable bank 0-1 in CGB Mode)
    public const ushort VRAM_StartAddress = 0x8000;

    //   A000-BFFF   8KB External RAM     (in cartridge, switchable bank, if any)
    public const ushort ExtRAM_StartAddress = 0xA000;

    //   C000-CFFF   4KB Work RAM Bank 0 (WRAM)
    public const ushort WRAM_0_StartAddress = 0xC000;

    //   D000-DFFF   4KB Work RAM Bank 1 (WRAM)  (switchable bank 1-7 in CGB Mode)
    public const ushort WRAM_1_StartAddress = 0xD000;

    //   E000-FDFF   Same as C000-DDFF (ECHO)    (typically not used)
    public const ushort WRAM_ECHO_StartAddress = 0xE000;

    //   FE00-FE9F   Sprite Attribute Table (OAM)
    public const ushort OAM_StartAddress = 0xFE00;

    //   FEA0-FEFF   Not Usable
    public const ushort Unusable_StartAddress = 0xFEA0;

    //   FF00-FF7F   I/O Ports
    public const ushort IO_StartAddress = 0xFF00;

    //   FF80-FFFE   High RAM (HRAM)
    public const ushort HRAM_StartAddress = 0xFF80;

    //   FFFF        Interrupt Enable Register
    public const ushort IE_address = 0xFFFF;
}