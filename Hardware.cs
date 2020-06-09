abstract class Hardware<T> where T : IBus
{
    protected T bus;

    public bool IsConnected => bus != null;

    virtual public void Connect(T bus)
    {
        this.bus = bus;
    }
}