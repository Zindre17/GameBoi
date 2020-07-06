
interface IMemoryRange
{
    IMemory this[Address address] { get; set; }
    Address Size { get; }
    Byte Read(Address address);
    void Write(Address address, Byte value);
}

class MemoryRange : IMemoryRange
{
    protected IMemory[] memory;

    public Address Size => memory.Length;

    public MemoryRange(Byte[] memory, bool isReadOnly = false)
    {
        this.memory = new IMemory[memory.Length];
        for (int i = 0; i < memory.Length; i++)
            this.memory[i] = new Register(memory[i], isReadOnly);
    }
    public MemoryRange(IMemory memory) => this.memory = new IMemory[] { memory };
    public MemoryRange(Address size, bool isReadOnly = false) => memory = Register.CreateMany(size, isReadOnly);

    public IMemory this[Address address]
    {
        get => memory[address];
        set => memory[address] = value;
    }

    public virtual Byte Read(Address address) => memory[address].Read();

    public virtual void Write(Address address, Byte value) => memory[address].Write(value);

}