using System.Windows.Input;

public class Controller : Hardware, IUpdateable
{

    const ushort P1_address = 0xFF00;

    private readonly P1 p1 = new P1();

    const Key A = Key.K;
    const Key B = Key.J;
    const Key Start = Key.Enter;
    const Key Select = Key.RightShift;
    const Key Up = Key.W;
    const Key Left = Key.A;
    const Key Down = Key.S;
    const Key Right = Key.D;

    Byte pad;
    Byte buttons;

    public void RegisterInputs()
    {
        buttons = 0xF;
        if (Keyboard.IsKeyDown(A)) buttons ^= (1 << 0);
        if (Keyboard.IsKeyDown(B)) buttons ^= (1 << 1);
        if (Keyboard.IsKeyDown(Select)) buttons ^= (1 << 2);
        if (Keyboard.IsKeyDown(Start)) buttons ^= (1 << 3);

        pad = 0xF;
        if (Keyboard.IsKeyDown(Right)) pad ^= (1 << 0);
        if (Keyboard.IsKeyDown(Left)) pad ^= (1 << 1);
        if (Keyboard.IsKeyDown(Up)) pad ^= (1 << 2);
        if (Keyboard.IsKeyDown(Down)) pad ^= (1 << 3);
    }


    public void CheckInputs()
    {
        if (p1.P14 == p1.P15) return;

        Byte newActive = 0x0F;
        if (p1.P15)
            newActive = buttons;
        else if (p1.P14)
            newActive = pad;

        if (p1.Active != newActive)
        {
            p1.SetActive(newActive);
            bus.RequestInterrupt(InterruptType.Joypad);
        }
    }

    public override void Connect(Bus bus)
    {
        base.Connect(bus);
        bus.ReplaceMemory(P1_address, p1);
    }

    public void Update(byte cycles)
    {
        CheckInputs();
    }
}
