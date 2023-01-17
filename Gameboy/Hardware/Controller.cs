using GB_Emulator.Memory;
using GB_Emulator.Memory.Specials;
using Silk.NET.Input;

namespace GB_Emulator.Hardware
{
    public class Controller : Hardware, IUpdatable
    {

        const ushort P1_address = 0xFF00;

        private readonly P1 p1 = new();

        const Key A = Key.K;
        const Key B = Key.J;
        const Key Start = Key.Enter;
        const Key Select = Key.ShiftRight;
        const Key Up = Key.W;
        const Key Left = Key.A;
        const Key Down = Key.S;
        const Key Right = Key.D;

        Byte pad = 0xff;
        Byte buttons = 0xff;


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

        public void Update(uint _, ulong __)
        {
            CheckInputs();
        }
    }
}
