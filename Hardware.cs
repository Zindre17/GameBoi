abstract class Hardware<T> where T : IBus
{
    protected T bus;

    public bool IsConnected => bus != null;

    virtual public void Connect(T bus)
    {
        this.bus = bus;
    }

    virtual protected byte Read(ushort address)
    {
        if (!bus.Read(address, out byte value))
            throw new MemoryReadException(address);
        return value;
    }

    virtual protected void Write(ushort address, byte value)
    {
        if (!bus.Write(address, value))
            throw new MemoryWriteException(address);
    }
}