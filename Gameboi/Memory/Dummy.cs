class DummyRange : IMemoryRange
{
    private IMemory dummy = new Dummy();
    public IMemory this[Address address] { get => dummy; set { } }

    public Address Size => throw new System.NotImplementedException();

    public Byte Read(Address address) => 0;

    public void Write(Address address, Byte value) { }
}

class Dummy : IMemory
{
    public Byte Read() => 0;
    public void Write(Byte value) { }
}