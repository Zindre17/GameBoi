using System;

class Bank : IMemory
{
    protected IMemory[] banks;
    protected byte pointer = 0;

    public Bank(byte amount, ushort size, bool isReadOnly = false)
    {
        if (amount == 0)
            throw new ArgumentOutOfRangeException();

        banks = new IMemory[amount];
        for (int i = 0; i < amount; i++)
        {
            banks[i] = new Memory(size, isReadOnly);
        }
    }

    public Bank(IMemory[] banks) { this.banks = banks; }

    public void Switch(byte to)
    {
        if (to < banks.Length)
            pointer = to;
        else
            throw new ArgumentOutOfRangeException();
    }

    public bool Read(Address address, out Byte value)
    {
        return banks[pointer].Read(address, out value);
    }

    public bool Write(Address address, Byte value)
    {
        return banks[pointer].Write(address, value);
    }
}
