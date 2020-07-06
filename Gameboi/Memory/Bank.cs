using System;

class Bank : IMemoryRange
{
    protected IMemoryRange[] banks;
    protected byte pointer = 0;

    public Address Size => banks[pointer].Size;


    public Bank(byte amount, ushort size, bool isReadOnly = false)
    {
        if (amount == 0)
        {
            banks = new IMemoryRange[] { new UnusedRange() };
        }
        else
        {
            banks = new IMemoryRange[amount];
            for (int i = 0; i < amount; i++)
                banks[i] = new MemoryRange(size, isReadOnly);
        }
    }

    public Bank(IMemoryRange[] banks) { this.banks = banks; }

    public void Switch(Byte to)
    {
        if (to < banks.Length)
            pointer = to;
        else
            throw new ArgumentOutOfRangeException();
    }

    public IMemory this[Address address]
    {
        get => banks[pointer][address];
        set => banks[pointer][address] = value;
    }

    public virtual Byte Read(Address address) => banks[pointer].Read(address);
    public virtual void Write(Address address, Byte value) => banks[pointer].Write(address, value);

}
