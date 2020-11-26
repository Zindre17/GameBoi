public class DIV : Register
{
    public override void Write(Byte value) => base.Write(0);
    public virtual void Bump() => data++;
}