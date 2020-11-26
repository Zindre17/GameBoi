public class TIMA : DIV
{
    public TIMA(OverflowHandler handler) : base()
    {
        OnOverflow += handler;
    }

    public delegate void OverflowHandler();

    public OverflowHandler OnOverflow;
    private bool hasOverflown = false;

    public override void Bump()
    {
        if (hasOverflown && OnOverflow != null) OnOverflow();
        else base.Bump();
        if (data == 0xFF) hasOverflown = true;
    }
}