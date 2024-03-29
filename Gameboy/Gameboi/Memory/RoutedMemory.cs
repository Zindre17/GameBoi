using System;

namespace GB_Emulator.Gameboi.Memory
{
    public class RoutedMemory : IMemoryRange
    {
        readonly Address offset;
        readonly IMemoryRange memory;
        private readonly Action<Address, Byte> onWrite;

        public RoutedMemory(Address offset, IMemoryRange memory, Action<Address, Byte> onWrite = null)
        {
            this.offset = offset;
            this.memory = memory;
            this.onWrite = onWrite;
        }

        public RoutedMemory(Address offset, IMemory memory) => (this.offset, this.memory) = (offset, new MemoryRange(memory));

        public Byte Read(Address address, bool isCpu = false) => memory.Read(address - offset, isCpu);
        public void Write(Address address, Byte value, bool isCpu = false)
        {
            onWrite?.Invoke(address - offset, value);
            memory.Write(address - offset, value, isCpu);
        }
        public void Set(Address address, IMemory replacement) => memory.Set(address - offset, replacement);

        public Address Size => memory.Size;

    }
}