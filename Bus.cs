using System;
using static MemoryAddresses;

class Bus
{
    private CPU cpu;
    private Cartridge game;

    private IMemoryRange[] memory = new IMemoryRange[0x10000];

    private IMemoryRange VRAM;
    private IMemoryRange WRAM_0;
    private IMemoryRange WRAM_1;
    private IMemoryRange OAM;
    private IMemoryRange IO;
    private IMemoryRange HRAM;
    private IMemory IE;


    public Bus()
    {
        VRAM = new MemoryRange(0x2000);
        RouteMemory(VRAM_StartAddress, VRAM);

        WRAM_0 = new MemoryRange(0x1000);
        RouteMemory(WRAM_0_StartAddress, WRAM_0);
        RouteMemory(WRAM_ECHO_StartAddress, WRAM_0);

        WRAM_1 = new MemoryRange(0x1000);
        RouteMemory(WRAM_1_StartAddress, WRAM_1);
        RouteMemory(WRAM_ECHO_StartAddress + WRAM_0.Size, WRAM_1);

        OAM = new MemoryRange(0xA0);
        RouteMemory(OAM_StartAddress, OAM);

        IO = new MemoryRange(0xA0);
        RouteMemory(IO_StartAddress, IO);

        HRAM = new MemoryRange(0x7F);
        RouteMemory(HRAM_StartAddress, HRAM);

        IE = new InterruptRegister();
        RouteMemory(IE_address, IE);

        Scramble(VRAM);

        Write(0xFF40, 0x91);// LCDC        
        Write(0xFF41, 0x85); // STAT
        Write(0xFF47, 0xFC); // BGP
        Write(0xFF48, 0xFF); //OBP0
        Write(0xFF49, 0xFF); //OBP1

    }
    private Random random = new Random();

    public void RouteMemory(Address startAddress, IMemoryRange memory)
    {
        var routedMemory = new RoutedMemory(startAddress, memory);
        Address end = startAddress + memory.Size;
        for (int current = startAddress; current < end; current++)
            this.memory[current] = routedMemory;
    }

    public void RouteMemory(Address address, IMemory memory) => this.memory[address] = new RoutedMemory(address, memory);

    public void Scramble(IMemoryRange range)
    {
        for (ushort i = 0; i < range.Size; i++)
            range[i].Write((byte)random.Next());
    }

    public void ConnectCartridge(Cartridge cartridge)
    {
        game = cartridge;
        RouteMemory(ROM_bank_0_StartAddress, cartridge.RomBank0);
        RouteMemory(ROM_bank_n_StartAddress, cartridge.RomBankN);
        RouteMemory(ExtRAM_StartAddress, cartridge.RamBankN);
    }


    public void ConnectCPU(CPU cpu)
    {
        this.cpu = cpu;
    }

    public void RequestInterrrupt(InterruptType type)
    {
        if (cpu != null)
            cpu.RequestInterrupt(type);
    }

    public Byte Read(Address address) => memory[address].Read(address);

    public void Write(Address address, Byte value) => memory[address].Write(address, value);

}
