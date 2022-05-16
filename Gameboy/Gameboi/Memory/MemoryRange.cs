namespace GB_Emulator.Gameboi.Memory
{
    public interface IMemoryRange
    {
        Address Size { get; }
        Byte Read(Address address, bool isCpu = false);
        void Write(Address address, Byte value, bool isCpu = false);
        void Set(Address address, IMemory replacement);
    }

    public class MemoryRange : IMemoryRange
    {
        protected IMemory[] memory;

        public Address Size => memory.Length;

        public MemoryRange(Byte[] memory, bool isReadOnly = false)
        {
            this.memory = new IMemory[memory.Length];
            for (int i = 0; i < memory.Length; i++)
                this.memory[i] = new Register(memory[i], isReadOnly);
        }
        public MemoryRange(IMemory[] memory) => this.memory = memory;
        public MemoryRange(IMemory memory) => this.memory = new IMemory[] { memory };
        public MemoryRange(Address size, bool isReadOnly = false) => memory = Register.CreateMany(size, isReadOnly);

        public void Set(Address address, IMemory replacement) => memory[address] = replacement;

        public virtual Byte Read(Address address, bool isCpu = false) => memory[address].Read();

        public virtual void Write(Address address, Byte value, bool isCpu = false) => memory[address].Write(value);

    }
}