
public class Bank : IMemoryRange
{
    protected IMemoryRange[] banks;
    protected Byte pointer = 0;

    public Address Size => banks[pointer].Size;


    public Bank(byte amount, ushort size, bool isReadOnly = false)
    {
        if (amount == 0)
        {
            banks = new IMemoryRange[] { new DummyRange() };
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
            pointer = 0;
    }

    public long GetTotalSize()
    {
        long result = 0;
        foreach (var bank in banks)
            result += bank.Size;
        return result;
    }

    public virtual Byte Read(Address address, bool isCpu = false) => banks[pointer].Read(address, isCpu);
    public virtual void Write(Address address, Byte value, bool isCpu = false) => banks[pointer].Write(address, value, isCpu);

    public void Set(Address address, IMemory replacement) => banks[pointer].Set(address, replacement);

}
