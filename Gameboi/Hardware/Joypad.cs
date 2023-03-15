using Gameboi.Memory.Specials;
using Silk.NET.Input;

namespace Gameboi.Hardware;

public class Joypad
{
    private readonly SystemState state;

    private const Key A = Key.K;
    private const Key B = Key.J;
    private const Key Start = Key.Enter;
    private const Key Select = Key.ShiftRight;
    private const Key Up = Key.W;
    private const Key Left = Key.A;
    private const Key Down = Key.S;
    private const Key Right = Key.D;

    private byte pad = 0xff;
    private byte buttons = 0xff;

    public Joypad(SystemState state)
    {
        this.state = state;
    }

    public void KeyUp(Key key)
    {
        switch (key)
        {
            case A: buttons |= 1 << 0; break;
            case B: buttons |= 1 << 1; break;
            case Select: buttons |= 1 << 2; break;
            case Start: buttons |= 1 << 3; break;

            case Right: pad |= 1 << 0; break;
            case Left: pad |= 1 << 1; break;
            case Up: pad |= 1 << 2; break;
            case Down: pad |= 1 << 3; break;
        }
    }

    public void KeyDown(Key key)
    {
        switch (key)
        {
            case A: buttons &= 0b1110; break;
            case B: buttons &= 0b1101; break;
            case Select: buttons &= 0b1011; break;
            case Start: buttons &= 0b0111; break;

            case Right: pad &= 0b1110; break;
            case Left: pad &= 0b1101; break;
            case Up: pad &= 0b1011; break;
            case Down: pad &= 0b0111; break;
        }
    }

    public void CheckInputs()
    {
        var p1 = new ImprovedP1(state.P1);
        if (p1.P14 == p1.P15) return;

        var currentPresses = p1.P15 ? buttons : pad;

        if (p1.CurrentPresses != currentPresses)
        {
            state.P1 = currentPresses;
            var interruptRequests = new InterruptState(state.InterruptFlags);
            state.InterruptFlags = interruptRequests.WithJoypadSet();
        }
    }
}
