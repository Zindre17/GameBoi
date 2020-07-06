abstract class Hardware
{
    protected Bus bus;

    public bool IsConnected => bus != null;

    public virtual void Connect(Bus bus) => this.bus = bus;

    public Byte Read(Address address) => bus.Read(address);
    public void Write(Address address, Byte value) => bus.Write(address, value);

}