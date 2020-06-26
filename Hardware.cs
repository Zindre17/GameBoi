abstract class Hardware<T> where T : IBus
{
    protected T bus;

    public bool IsConnected => bus != null;

    virtual public void Connect(T bus)
    {
        this.bus = bus;
    }

    virtual protected Byte Read(Address address)
    {
        if (!bus.Read(address, out Byte value))
            throw new MemoryReadException(address);
        return value;
    }

    virtual protected void Write(Address address, Byte value)
    {
        if (!bus.Write(address, value))
            throw new MemoryWriteException(address);
    }
}