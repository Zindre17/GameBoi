abstract class Memory
{
    private bool isReadOnly;
    public bool IsReadOnly => isReadOnly;

    private byte[] memory;

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