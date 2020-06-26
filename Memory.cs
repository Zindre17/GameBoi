using System;

interface IMemory
{
    bool Read(Address address, out Byte value);
    bool Write(Address address, Byte value);
}

class Memory : IMemory
{
    private bool isReadOnly;

    protected byte[] memory;

    public int Size => memory.Length;

    public Memory(ushort size, bool isReadOnly = false)
    {
        this.isReadOnly = isReadOnly;
        memory = new byte[size];
    }

    public Memory(byte[] data, bool isReadOnly = false)
    {
        this.isReadOnly = isReadOnly;
        memory = new byte[data.Length];
        data.CopyTo(memory, 0);
    }

    public virtual bool Write(Address address, Byte value)
    {
        if (isReadOnly) return false;
        memory[address] = value;
        return true;
    }
    public virtual bool Read(Address address, out Byte value)
    {
        value = memory[address];
        return true;
    }
}

public class MemoryReadException : Exception
{
    public MemoryReadException(ushort address) : base($"Failed to read from memory. Address: {address}") { }
}

public class MemoryWriteException : Exception
{
    public MemoryWriteException(ushort address) : base($"Failed to write to memory. Address: {address}") { }
}
