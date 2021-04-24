using System;
using System.Collections.Generic;
using static GeneralMemoryMap;

public class Bus
{

    private CPU cpu;
    private SPU spu;

    private ulong cycles = 0;
    private byte cyclesSinceLast = 0;
    public ulong Cycles => cycles;

    private readonly List<IUpdateable> updateables = new List<IUpdateable>();

    public void UpdateAll()
    {
        foreach (var component in updateables)
            component.Update(cyclesSinceLast);
    }

    public void UpdateCycles(Byte cyclesToAdd)
    {
        cyclesSinceLast = cyclesToAdd;
        UpdateAll();
        cycles += cyclesToAdd;
    }

    public void AddNextFrameOfSamples()
    {
        spu.AddNextSamples();
    }

    private readonly IMemoryRange[] memory = new IMemoryRange[0x10000];

    private IMemoryRange vram;
    public void SetVram(IMemoryRange vram)
    {
        this.vram = vram;
        RouteMemory(VRAM_StartAddress, this.vram, VRAM_EndAddress);
    }
    private readonly IMemoryRange wram_0;
    private readonly IMemoryRange wram_1;
    private IMemoryRange oam;
    public void SetOam(IMemoryRange oam)
    {
        this.oam = oam;
        RouteMemory(OAM_StartAddress, this.oam, OAM_EndAddress);
    }
    private readonly IMemoryRange io;
    private readonly IMemoryRange hram;
    private readonly IMemory ie;

    private readonly IMemoryRange unusable = new DummyRange();

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

    public void ReplaceMemory(Address address, IMemory memory)
    {
        this.memory[address].Set(address, memory);
    }

    public void RouteMemory(Address startAddress, IMemoryRange memory) => RouteMemory(startAddress, memory, startAddress + memory.Size);

    public void RouteMemory(Address address, IMemory memory) => this.memory[address] = new RoutedMemory(address, memory);

    public void RouteMemory(Address startAddress, IMemoryRange memory, Address endAddress)
    {
        var routedMemory = new RoutedMemory(startAddress, memory);
        for (int current = startAddress; current < endAddress; current++)
            this.memory[current] = routedMemory;
    }

    public void ConnectCartridge(Cartridge cartridge)
    {
        RouteMemory(ROM_bank_0_StartAddress, cartridge.RomBank0);
        RouteMemory(ROM_bank_n_StartAddress, cartridge.RomBankN);
        RouteMemory(ExtRAM_StartAddress, cartridge.RamBankN, ExtRAM_EndAddress);
    }

    public void RequestInterrupt(InterruptType type)
    {
        if (cpu != null)
            cpu.RequestInterrupt(type);
    }

    public Byte Read(Address address, bool isCpu = false) => memory[address].Read(address, isCpu);

    public void Write(Address address, Byte value, bool isCpu = false) => memory[address].Write(address, value, isCpu);

    public void Connect(Hardware component)
    {
        if (component is CPU _cpu)
        {
            cpu = _cpu;
        }
        else if (component is SPU _spu)
        {
            spu = _spu;
        }
        else if (component is IUpdateable updateable)
        {
            updateables.Add(updateable);
        }
        component.Connect(this);
    }

}
