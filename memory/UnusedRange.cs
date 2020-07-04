class UnusedRange : IMemoryRange
{
    public IMemory this[Address address] { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public Address Size => 0;

    public Byte Read(Address address) => 0;

    public void Write(Address address, Byte value) { }

}
