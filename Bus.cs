using System;
using System.Text;

class MainBus : IBus
{
    private Cartridge game;
    private string SerialTransfer = "";
    public MainBus()
    {
        VRAM = new Memory(0x2000);
        WRAM_0 = new Memory(0x1000);
        WRAM_1 = new Memory(0x1000);
        OAM = new Memory(0xA0);
        IO = new Memory(0x80);
        HRAM = new Memory(0x7F);

        //set reset values for special registers

        Write(0xFF40, 0x91);// LCDC        
        Write(0xFF41, 2); // STAT
        Write(0xFF47, 0xFC); // BGP
        Write(0xFF48, 0xFF); //OBP0
        Write(0xFF49, 0xFF); //OBP1

        ScrambleVRAM();
    }
    private Random random = new Random();

    public void ScrambleVRAM()
    {
        for (ushort i = 0; i < VRAM.Size; i++)
        {
            VRAM.Write(i, (byte)random.Next());
            // if (i < 0x1000)
            //     WRAM_0.Write(i, (byte)random.Next());
            // else
            //     WRAM_1.Write((ushort)(i - 0x1000), (byte)random.Next());
        }
    }

    public void ConnectCartridge(Cartridge cartridge)
    {
        game = cartridge;
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

    //   4000-7FFF   16KB ROM Bank 01..NN (in cartridge, switchable bank number)
    const ushort ROM_bank_n_StartAddress = 0x4000;

    //   8000-9FFF   8KB Video RAM (VRAM) (switchable bank 0-1 in CGB Mode)
    const ushort VRAM_StartAddress = 0x8000;
    private Memory VRAM;

    //   A000-BFFF   8KB External RAM     (in cartridge, switchable bank, if any)
    const ushort ExtRAM_StartAddress = 0xA000;
    private Memory ExtRAM;

    //   C000-CFFF   4KB Work RAM Bank 0 (WRAM)
    const ushort WRAM_0_StartAddress = 0xC000;
    private Memory WRAM_0;

    //   D000-DFFF   4KB Work RAM Bank 1 (WRAM)  (switchable bank 1-7 in CGB Mode)
    const ushort WRAM_1_StartAddress = 0xD000;
    private Memory WRAM_1;

    //   E000-FDFF   Same as C000-DDFF (ECHO)    (typically not used)
    const ushort WRAM_ECHO_StartAddress = 0xE000;
    // private RAM WRAM_ECHO;

    //   FE00-FE9F   Sprite Attribute Table (OAM)
    const ushort OAM_StartAddress = 0xFE00;
    private Memory OAM;

    //   FEA0-FEFF   Not Usable
    const ushort Unusable_StartAddress = 0xFEA0;

    //   FF00-FF7F   I/O Ports
    const ushort IO_StartAddress = 0xFF00;
    private Memory IO;

    //   FF80-FFFE   High RAM (HRAM)
    const ushort HRAM_StartAddress = 0xFF80;
    private Memory HRAM;

    //   FFFF        Interrupt Enable Register
    private Register IE;

    public bool Read(Address address, out Byte value)
    {
        if (address == 0xFF00)
        {
            value = 0x0F;
            return true;
        }
        IMemory mem = GetLocation(address, out ushort relativeAddress);

        if (mem == null)
        {
            value = 0;
            return true;
        }
        return mem.Read(relativeAddress, out value);
    }

    public bool Write(Address address, Byte value)
    {
        if (address == 0xff01)
        {
            SerialTransfer += Encoding.ASCII.GetString(new byte[] { value });
        }
        if (address == 0xff46)
        {
            //start DMA transfer
            ushort start = (ushort)(value / 0x100);
            ushort startOut = 0xff80;
            for (int i = 0; i < 160; i++)
            {
                Read(start++, out Byte data);
                Write(startOut++, data);
            }
            return true;
        }
        else
        {
            IMemory mem = GetLocation(address, out ushort relativeAddress);

            if (mem == null) return true;

            //any write to DIV makes it 0
            if (address == 0xFF04) value = 0;
            if (address == 0xFF00) value = 0xFF;

            return mem.Write(relativeAddress, value);
        }
    }

    private IMemory GetLocation(ushort address, out ushort relativeAddress)
    {
        // in cartridge
        if (address < VRAM_StartAddress)
        {
            relativeAddress = address;
            return game;
        }

        // VRAM
        if (address < ExtRAM_StartAddress)
        {
            relativeAddress = (ushort)(address - VRAM_StartAddress);
            return VRAM;
        }

        // External RAM
        if (address < WRAM_0_StartAddress)
        {
            relativeAddress = address;
            return game;
        }

        // WorkRAM bank 0
        if (address < WRAM_1_StartAddress)
        {
            relativeAddress = (ushort)(address - WRAM_0_StartAddress);
            return WRAM_0;
        }

        // WorkRAM bank 1
        if (address < WRAM_ECHO_StartAddress)
        {
            relativeAddress = (ushort)(address - WRAM_1_StartAddress);
            return WRAM_1;
        }

        // Mirror of WorkRAM
        if (address < OAM_StartAddress)
        {
            // Emulation shortcut
            relativeAddress = (ushort)(address - WRAM_ECHO_StartAddress);
            return WRAM_1;
        }

        // Sprite Attribute Table (OAM)
        if (address < Unusable_StartAddress)
        {
            relativeAddress = (ushort)(address - OAM_StartAddress);
            return OAM;
        }

        // Not Usable
        if (address < IO_StartAddress)
        {
            relativeAddress = 0;
            return null;
        }

        // I/O ports
        if (address < HRAM_StartAddress)
        {
            relativeAddress = (ushort)(address - IO_StartAddress);
            return IO;
        }

        // HRAM
        if (address < 0xFFFF)
        {
            relativeAddress = (ushort)(address - HRAM_StartAddress);
            return HRAM;
        }
        else
        {
            relativeAddress = 0;
            return IE;
        }
    }
}

interface IBus
{
    bool Read(Address address, out Byte value);
    bool Write(Address address, Byte value);
}