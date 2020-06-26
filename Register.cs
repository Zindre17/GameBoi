class Register : IMemory
{

    byte value;

    public bool IsReadOnly => false;

    public Register() { }

    public bool Read(Address address, out Byte value)
    {
        value = this.value;
        return true;
    }

    public bool Write(Address address, Byte value)
    {
        this.value = value;
        return true;
    }
}