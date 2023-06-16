using Gameboi.Io;
using Gameboi.Processor;
using Silk.NET.Input;

namespace Gameboi.Controls;

public class Controller
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

    public Controller(SystemState state)
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
        var controller = new ControllerState(state.P1);
        if (controller.IsInPadMode == controller.IsInButtonMode) return;

        var currentPresses = controller.IsInButtonMode ? buttons : pad;

        if (controller.CurrentPresses != currentPresses)
        {
            state.P1 = currentPresses;
            var interruptRequests = new InterruptState(state.InterruptFlags);
            state.InterruptFlags = interruptRequests.WithJoypadSet();
        }
    }
}
