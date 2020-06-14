class MainBus : IBus
{
    public MainBus()
    {
        //assume no cartridge yet
        ROM_bank_0 = null;
        ROM_bank_n = null;

        VRAM = new RAM(0x2000);

        ExtRAM = null;
        WRAM_0 = new RAM(0x1000);
        WRAM_1 = new RAM(0x1000);
        WRAM_ECHO = new RAM(0x1E00);
        OAM = new RAM(0x50);
        IO = new RAM(0x80);
        HRAM = new RAM(0x6F);
    }

    //not sure about this...
    public void ConnectCartridge(Cartridge cartridge)
    {
        ROM_bank_0 = cartridge.ROM_Bank0;
        ROM_bank_n = cartridge.ROM_BankN;
        ExtRAM = cartridge.RAM;
    }

    private CPU cpu;

    public void ConnectCPU(CPU cpu)
    {
        this.cpu = cpu;
    }

    public void RequestInterrrupt(InterruptType type)
    {
        if (cpu != null)
            cpu.RequestInterrupt(type);
    }


    //  General Memory Map
    //   0000-3FFF   16KB ROM Bank 00     (in cartridge, fixed at bank 00)
    const ushort ROM_bank_0_StartAddress = 0;
    private ROM ROM_bank_0;

    //   4000-7FFF   16KB ROM Bank 01..NN (in cartridge, switchable bank number)
    const ushort ROM_bank_n_StartAddress = 0x4000;
    private Memory ROM_bank_n;

    //   8000-9FFF   8KB Video RAM (VRAM) (switchable bank 0-1 in CGB Mode)
    const ushort VRAM_StartAddress = 0x8000;
    private RAM VRAM;

    //   A000-BFFF   8KB External RAM     (in cartridge, switchable bank, if any)
    const ushort ExtRAM_StartAddress = 0xA000;
    private RAM ExtRAM;

    //   C000-CFFF   4KB Work RAM Bank 0 (WRAM)
    const ushort WRAM_0_StartAddress = 0xC000;
    private RAM WRAM_0;

    //   D000-DFFF   4KB Work RAM Bank 1 (WRAM)  (switchable bank 1-7 in CGB Mode)
    const ushort WRAM_1_StartAddress = 0xD000;
    private RAM WRAM_1;

    //   E000-FDFF   Same as C000-DDFF (ECHO)    (typically not used)
    const ushort WRAM_ECHO_StartAddress = 0xE000;
    private RAM WRAM_ECHO;

    //   FE00-FE9F   Sprite Attribute Table (OAM)
    const ushort OAM_StartAddress = 0xFE00;
    private RAM OAM;

    //   FEA0-FEFF   Not Usable
    const ushort Unusable_StartAddress = 0xFEA0;

    //   FF00-FF7F   I/O Ports
    const ushort IO_StartAddress = 0xFF00;
    private RAM IO;

    //   FF80-FFFE   High RAM (HRAM)
    const ushort HRAM_StartAddress = 0xFF80;
    private RAM HRAM;

    //   FFFF        Interrupt Enable Register
    private byte IE;

    public bool Read(ushort address, out byte value)
    {
        if (address == 0xFFFF)
        {
            value = IE;
            return true;
        }
        else
        {
            Memory mem = GetLocation(address, out ushort relativeAddress);

            if (mem == null)
            {
                value = 0;
                return false;
            }
            return mem.Read(relativeAddress, out value);
        }

    }

    public bool Write(ushort address, byte value)
    {
        // Is not writable area
        if (address < ROM_bank_n_StartAddress) return false;
        if (address < VRAM_StartAddress && ROM_bank_n.IsReadOnly) return false;

        // Is writable area
        if (address == 0xFFFF)
        {
            IE = value;
            return true;
        }
        else
        {
            Memory mem = GetLocation(address, out ushort relativeAddress);

            if (mem == null) return false;

            //any write to DIV makes it 0
            if (address == 0xFF04) value = 0;

            return mem.Write(relativeAddress, value);
        }
    }

    private Memory GetLocation(ushort address, out ushort relativeAddress)
    {
        Memory NoLocation(out ushort x)
        {
            x = 0;
            return null;
        }

        // ROM bank 00 in Cartridge
        if (address < ROM_bank_n_StartAddress)
        {
            relativeAddress = (ushort)(address ^ ROM_bank_0_StartAddress);
            return ROM_bank_0;
        }

        // ROM bank 01..NN in Cartridge
        if (address < VRAM_StartAddress)
        {
            relativeAddress = (ushort)(address ^ ROM_bank_n_StartAddress);
            return ROM_bank_n;
        }

        // VRAM
        if (address < ExtRAM_StartAddress)
        {
            relativeAddress = (ushort)(address ^ VRAM_StartAddress);
            return VRAM;
        }

        // External RAM
        if (address < WRAM_0_StartAddress)
        {
            relativeAddress = (ushort)(address ^ ExtRAM_StartAddress);
            return ExtRAM;
        }

        // WorkRAM bank 0
        if (address < WRAM_1_StartAddress)
        {
            relativeAddress = (ushort)(address ^ WRAM_0_StartAddress);
            return WRAM_0;
        }

        // WorkRAM bank 1
        if (address < WRAM_ECHO_StartAddress)
        {
            relativeAddress = (ushort)(address ^ WRAM_1_StartAddress);
            return VRAM;
        }

        // Mirror of WorkRAM
        if (address < OAM_StartAddress)
        {
            // Emulation shortcut
            relativeAddress = (ushort)(address ^ WRAM_ECHO_StartAddress);
            return WRAM_ECHO;
        }

        // Sprite Attribute Table (OAM)
        if (address < Unusable_StartAddress)
        {
            relativeAddress = (ushort)(address ^ OAM_StartAddress);
            return OAM;
        }

        // Not Usable
        if (address < IO_StartAddress)
        {
            return NoLocation(out relativeAddress);
        }

        // I/O ports
        if (address < HRAM_StartAddress)
        {
            relativeAddress = (ushort)(address ^ IO_StartAddress);
            return IO;
        }

        // HRAM
        if (address < 0xFFFF)
        {
            relativeAddress = (ushort)(address ^ VRAM_StartAddress);
            return VRAM;
        }

        // address is 0xFFFF which is the IE Register...
        return NoLocation(out relativeAddress);
    }
}

interface IBus
{
    bool Read(ushort address, out byte value);
    bool Write(ushort address, byte value);
}