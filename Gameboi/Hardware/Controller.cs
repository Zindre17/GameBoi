using System.Windows.Input;

class Controller : Hardware
{

    const ushort P1_address = 0xFF00;

    private P1 p1 = new P1();

    const Key A = Key.K;
    const Key B = Key.J;
    const Key Start = Key.Enter;
    const Key Select = Key.RightShift;
    const Key Up = Key.W;
    const Key Left = Key.A;
    const Key Down = Key.S;
    const Key Right = Key.D;

    public void CheckInputs()
    {
        Byte newState = 0x0F;
        if (p1.P15)
        {
            if (Keyboard.IsKeyDown(A)) newState ^= (1 << 0);
            if (Keyboard.IsKeyDown(B)) newState ^= (1 << 1);
            if (Keyboard.IsKeyDown(Select)) newState ^= (1 << 2);
            if (Keyboard.IsKeyDown(Start)) newState ^= (1 << 3);
        }
        else if (p1.P14)
        {
            if (Keyboard.IsKeyDown(Right)) newState ^= (1 << 0);
            if (Keyboard.IsKeyDown(Left)) newState ^= (1 << 1);
            if (Keyboard.IsKeyDown(Up)) newState ^= (1 << 2);
            if (Keyboard.IsKeyDown(Down)) newState ^= (1 << 3);
        }
        newState |= (p1.Read() & 0xF0);
        p1.Write(newState);
        if (p1.Read() != newState)
        {
            bus.RequestInterrrupt(InterruptType.Joypad);
        }
    }

    public override void Connect(Bus bus)
    {
        base.Connect(bus);
        bus.ReplaceMemory(P1_address, p1);
    }
}
