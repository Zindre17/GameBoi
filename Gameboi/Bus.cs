using System;
using static GeneralMemoryMap;

class Bus
{
    private CPU cpu;
    private Cartridge game;

    private IMemoryRange[] memory = new IMemoryRange[0x10000];

    private IMemoryRange vram;
    public void SetVram(IMemoryRange vram) => this.vram = vram;
    private IMemoryRange wram_0;
    private IMemoryRange wram_1;
    private IMemoryRange oam;
    public void SetOam(IMemoryRange oam) => this.oam = oam;
    private IMemoryRange io;
    private IMemoryRange hram;
    private IMemory ie;

    private IMemoryRange unusable = new DummyRange();


    public Bus()
    {

        wram_0 = new MemoryRange(0x1000);
        RouteMemory(WRAM_0_StartAddress, wram_0);

        wram_1 = new MemoryRange(0x1000);
        RouteMemory(WRAM_1_StartAddress, wram_1);

        RouteMemory(WRAM_ECHO_StartAddress, wram_0);
        RouteMemory(WRAM_ECHO_StartAddress + wram_0.Size, wram_1, OAM_StartAddress);

        io = new MemoryRange(0xA0);
        RouteMemory(IO_StartAddress, io);

        RouteMemory(Unusable_StartAddress, unusable, Unusable_EndAddress);

        hram = new MemoryRange(0x7F);
        RouteMemory(HRAM_StartAddress, hram);

        ie = new InterruptRegister();
        RouteMemory(IE_address, ie);

    }
    private Random random = new Random();

    public void ReplaceMemory(Address address, IMemory memory)
    {
        this.memory[address][address] = memory;
    }

    public void RouteMemory(Address startAddress, IMemoryRange memory) => RouteMemory(startAddress, memory, startAddress + memory.Size);

    public void RouteMemory(Address address, IMemory memory) => this.memory[address] = new RoutedMemory(address, memory);

    public void RouteMemory(Address startAddress, IMemoryRange memory, Address endAddress)
    {
        var routedMemory = new RoutedMemory(startAddress, memory);
        for (int current = startAddress; current < endAddress; current++)
            this.memory[current] = routedMemory;
    }
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
        RouteMemory(ExtRAM_StartAddress, cartridge.RamBankN, ExtRAM_EndAddress);
    }

    public void ToggleVRAM(bool on)
    {
        RouteMemory(VRAM_StartAddress, on ? vram : unusable, VRAM_EndAddress);
    }

    public void ToggleOAM(bool on)
    {
        RouteMemory(OAM_StartAddress, on ? oam : unusable, OAM_EndAddress);
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
