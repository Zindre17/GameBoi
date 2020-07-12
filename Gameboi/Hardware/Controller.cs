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
        if (p1.P14 == p1.P15) return;

        Byte newActive = 0x0F;
        if (p1.P15)
        {
            if (Keyboard.IsKeyDown(A)) newActive ^= (1 << 0);
            if (Keyboard.IsKeyDown(B)) newActive ^= (1 << 1);
            if (Keyboard.IsKeyDown(Select)) newActive ^= (1 << 2);
            if (Keyboard.IsKeyDown(Start)) newActive ^= (1 << 3);
        }
        else if (p1.P14)
        {
            if (Keyboard.IsKeyDown(Right)) newActive ^= (1 << 0);
            if (Keyboard.IsKeyDown(Left)) newActive ^= (1 << 1);
            if (Keyboard.IsKeyDown(Up)) newActive ^= (1 << 2);
            if (Keyboard.IsKeyDown(Down)) newActive ^= (1 << 3);
        }

        if (p1.Active != newActive)
        {
            p1.SetActive(newActive);
            bus.RequestInterrrupt(InterruptType.Joypad);
        }
    }

    public override void Connect(Bus bus)
    {
        base.Connect(bus);
        bus.ReplaceMemory(P1_address, p1);
    }
}
