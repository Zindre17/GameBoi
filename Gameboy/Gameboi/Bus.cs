using System;
using System.Collections.Generic;
using GB_Emulator.Gameboi.Hardware;
using GB_Emulator.Gameboi.Memory;
using GB_Emulator.Gameboi.Memory.Specials;
using static GB_Emulator.Statics.GeneralMemoryMap;
using Byte = GB_Emulator.Gameboi.Memory.Byte;

namespace GB_Emulator.Gameboi
{
    public class Bus
    {

        private CPU cpu;

        private ulong cycles = 0;
        private byte cyclesSinceLast = 0;
        public ulong Cycles => cycles;

        private readonly List<IUpdatable> updatables = new();

        public void UpdateAll(ulong speed)
        {
            foreach (var component in updatables)
                component.Update(cyclesSinceLast, speed);
        }

        public void UpdateCycles(Byte cyclesToAdd, ulong speed)
        {
            cyclesSinceLast = cyclesToAdd;
            UpdateAll(speed);
            cycles += cyclesToAdd;
        }

        private readonly IMemoryRange[] memory = new IMemoryRange[0x10000];

        private IMemoryRange vram;
        public void SetVram(IMemoryRange vram)
        {
            this.vram = vram;
            RouteMemory(VRAM_StartAddress, this.vram, VRAM_EndAddress);
        }
        private readonly IMemoryRange wram_0;
        private readonly Bank wram_1;
        private readonly IMemory wramSwitch;
        private IMemoryRange oam;
        public void SetOam(IMemoryRange oam)
        {
            this.oam = oam;
            RouteMemory(OAM_StartAddress, this.oam, OAM_EndAddress);
        }
        private readonly IMemoryRange io;
        private readonly IMemoryRange hram;

        internal ulong GetCpuSpeed()
        {
            return cpu.GetSpeed();
        }

        private readonly IMemory ie;

        private readonly IMemoryRange unusable = new DummyRange();

        private void SwitchBank(Byte value)
        {
            if (value == 0)
                value++;
            wram_1.Switch((value & 7) - 1);
        }

        public Bus()
        {

            wram_0 = new MemoryRange(0x1000);
            RouteMemory(WRAM_0_StartAddress, wram_0);

            wram_1 = new Bank(7, 0x1000);
            RouteMemory(WRAM_1_StartAddress, wram_1);
            wramSwitch = new WriteTriggerRegister(SwitchBank);
            RouteMemory(WRAM_1_SwitchAddress, wramSwitch);

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

        public void RouteMemory(Address startAddress, IMemoryRange memory, System.Action<Address, Byte> onWrite = null)
            => RouteMemory(startAddress, memory, startAddress + memory.Size, onWrite);

        public void RouteMemory(Address address, IMemory memory) => this.memory[address] = new RoutedMemory(address, memory);

        public void RouteMemory(Address startAddress, IMemoryRange memory, Address endAddress, System.Action<Address, Byte> onWrite = null)
        {
            var routedMemory = new RoutedMemory(startAddress, memory, onWrite);
            for (int current = startAddress; current < endAddress; current++)
                this.memory[current] = routedMemory;
        }

        public void RequestInterrupt(InterruptType type)
        {
            cpu?.RequestInterrupt(type);
        }

        public Byte Read(Address address, bool isCpu = false)
        {
            if (!isCpu || (cpuReadFilter?.Invoke(address) ?? true))
                return memory[address].Read(address, isCpu);
            return 0xFF;

        }

        public void Write(Address address, Byte value, bool isCpu = false)
        {
            if (!isCpu || (cpuWriteFilter?.Invoke(address) ?? true))
                memory[address].Write(address, value, isCpu);
        }

        public void Connect(Hardware.Hardware component)
        {
            if (component is CPU _cpu)
            {
                cpu = _cpu;
            }
            else if (component is IUpdatable updatable)
            {
                updatables.Add(updatable);
            }
            component.Connect(this);
        }

        public void RegisterUpdatable(IUpdatable updateable)
        {
            if (!updatables.Contains(updateable))
            {
                updatables.Add(updateable);
            }
        }

        private Predicate<Address> cpuReadFilter = null;
        private Predicate<Address> cpuWriteFilter = null;
        public void SetCpuReadFilter(Predicate<Address> predicate) => cpuReadFilter = predicate;
        public void SetCpuWriteFilter(Predicate<Address> predicate) => cpuWriteFilter = predicate;
        public void ClearCpuReadFilter() => cpuReadFilter = null;
        public void ClearCpuWriteFilter() => cpuWriteFilter = null;

    }
}