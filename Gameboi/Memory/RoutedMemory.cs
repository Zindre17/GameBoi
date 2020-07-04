class RoutedMemory : IMemoryRange
{
    Address offset;
    IMemoryRange memory;

    public RoutedMemory(Address offset, IMemoryRange memory) => (this.offset, this.memory) = (offset, memory);
    public RoutedMemory(Address offset, IMemory memory) => (this.offset, this.memory) = (offset, new MemoryRange(memory));

    public Byte Read(Address address) => memory.Read(address - offset);
    public void Write(Address address, Byte value) => memory.Write(address - offset, value);

    public Address Size => memory.Size;
    public IMemory this[Address address]
    {
        get => memory[address - offset];
        set => memory[address - offset] = value;
    }

}