using System;
using System.Windows.Input;

class Controller : Hardware<MainBus>
{

    const ushort P1_address = 0xFF00;

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
        byte state = ReadP1();
        Byte newState = 0;
        if (ReadP15(state))
        {
            if (Keyboard.IsKeyDown(A)) newState |= (1 << 0);
            if (Keyboard.IsKeyDown(B)) newState |= (1 << 1);
            if (Keyboard.IsKeyDown(Select)) newState |= (1 << 2);
            if (Keyboard.IsKeyDown(Start)) newState |= (1 << 3);
        }
        else if (ReadP14(state))
        {
            if (Keyboard.IsKeyDown(Right)) newState |= (1 << 0);
            if (Keyboard.IsKeyDown(Left)) newState |= (1 << 1);
            if (Keyboard.IsKeyDown(Up)) newState |= (1 << 2);
            if (Keyboard.IsKeyDown(Down)) newState |= (1 << 3);
        }
        newState = (~newState & 0x0F) | (state & 0xF0);
        bus.Write(P1_address, newState);
        if (state != newState)
        {
            Console.WriteLine(newState);
            bus.RequestInterrrupt(InterruptType.Joypad);
        }
    }

    private byte ReadP1()
    {
        if (bus == null) throw new Exception("Controller not connected");

        bus.Read(P1_address, out Byte result);
        return result;
    }

    private bool ReadP14(byte p1_state)
    {
        //invert since 0 is active/pressed 
        return ((~p1_state) & (1 << 4)) != 0;
    }

    private bool ReadP15(byte p1_state)
    {
        return ((~p1_state) & (1 << 5)) != 0;
    }
}