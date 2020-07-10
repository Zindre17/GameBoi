class DummyRange : IMemoryRange
{
    private IMemory dummy = new Dummy();
    public IMemory this[Address address] { get => dummy; set { } }

    public Address Size => throw new System.NotImplementedException();

    public Byte Read(Address address, bool isCpu = false) => 0xFF;
    public void Write(Address address, Byte value, bool isCpu = false) { }

    public void Set(Address address, IMemory replacement) { }
}

class Dummy : IMemory
{
    public Byte Read() => 0xFF;
    public void Write(Byte value) { }
}