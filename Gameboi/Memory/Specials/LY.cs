namespace Gameboi.Memory.Specials;

public class LY : WriteTriggerRegister
{
    public LY(OnWriteHandler handler) : base(handler) { }

    public override void Write(Byte value) => base.Write(0);

    public void Increment() => base.Write(data + 1);

    public Byte Y => data;

    public void Reset() => base.Write(0);

    public void Set(Byte value) => base.Write(value);

}

