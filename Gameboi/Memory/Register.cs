class Register : IMemory
{
    bool isReadOnly;
    protected Byte data = 0;

    public Register(bool isReadOnly = false) : this(0, isReadOnly) { }

    public Register(Byte initialValue, bool isReadOnly = false) => (data, this.isReadOnly) = (initialValue, isReadOnly);

    public virtual void Write(Byte value)
    {
        if (isReadOnly) return;
        data = value;
    }

    public Byte Read() => data;

    public override string ToString() => data.ToString();

    public static Register[] CreateMany(Address amount, bool isReadOnly = false)
    {
        Register[] registers = new Register[amount];

        for (int i = 0; i < amount; i++)
            registers[i] = new Register(isReadOnly);

        return registers;
    }

}
