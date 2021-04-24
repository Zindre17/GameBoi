public class WriteTriggerRegister : Register
{

    public WriteTriggerRegister(OnWriteHandler handler) : base() => OnWrite = handler;

    public delegate void OnWriteHandler(Byte value);

    public OnWriteHandler OnWrite;

    public override void Write(Byte value)
    {
        base.Write(value);
        OnWrite?.Invoke(value);
    }
}
