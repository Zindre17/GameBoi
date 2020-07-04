class TIMA : DIV
{
    public TIMA(OverflowHandler handler) : base()
    {
        OnOverflow += handler;
    }

    public delegate void OverflowHandler();

    public OverflowHandler OnOverflow;
    private bool hasOverflown = false;

    public override void Tick()
    {
        if (hasOverflown && OnOverflow != null) OnOverflow();
        else base.Tick();
        if (data == 0xFF) hasOverflown = true;
    }
}