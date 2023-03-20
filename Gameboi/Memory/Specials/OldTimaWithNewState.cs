namespace Gameboi.Memory.Specials;

public class OldTimaWithNewState
{
    private readonly SystemState state;

    public OldTimaWithNewState(SystemState state, OverflowHandler handler) : base()
    {
        this.state = state;
        OnOverflow += handler;
    }

    public delegate void OverflowHandler();

    public OverflowHandler OnOverflow;

    public void Bump()
    {
        if (state.Tima is 0xFF)
        {
            OnOverflow?.Invoke();
        }
        state.Tima++;
    }
}

