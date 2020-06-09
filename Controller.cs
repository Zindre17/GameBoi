using System;

class Controller : Hardware<IBus>
{

    const ushort P1_address = 0xFF00;
    public enum Button
    {
        Left, Right, Up, Down,
        A, B, Start, Select
    }

    public void Press(Button button)
    {
        byte state = ReadP1();
        bool p15 = ReadP15(state);
        bool p14 = ReadP14(state);

        byte newState = (byte)(state & 0xF0); //copy first 4 bits of current state
        if (p15)
        {
            if (button == Button.A) newState |= (1 << 0);
            if (button == Button.B) newState |= (1 << 1);
            if (button == Button.Select) newState |= (1 << 2);
            if (button == Button.Start) newState |= (1 << 3);
        }
        else if (p14)
        {
            if (button == Button.Right) newState |= (1 << 0);
            if (button == Button.Left) newState |= (1 << 1);
            if (button == Button.Up) newState |= (1 << 2);
            if (button == Button.Down) newState |= (1 << 3);
        }

        bus.Write(P1_address, newState);
    }

    private byte ReadP1()
    {
        if (bus == null) throw new Exception("Controller not connected");

        bus.Read(P1_address, out byte result);
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