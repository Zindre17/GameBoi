using System;

abstract class Memory
{
    private bool isReadOnly;
    public bool IsReadOnly => isReadOnly;

    protected byte[] memory;

    public int Size => memory.Length;

    public Memory(ushort size, bool isReadOnly = false)
    {
        this.isReadOnly = isReadOnly;
        memory = new byte[size];
    }

    public virtual bool Write(ushort address, byte value)
    {
        if (isReadOnly) return false;
        memory[address] = value;
        return true;
    }
    public virtual bool Read(ushort address, out byte value)
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
