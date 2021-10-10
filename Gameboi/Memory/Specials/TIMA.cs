namespace GB_Emulator.Gameboi.Memory.Specials
{
    public class TIMA : Register
    {
        public TIMA(OverflowHandler handler) : base()
        {
            OnOverflow += handler;
        }

        public delegate void OverflowHandler();

        public OverflowHandler OnOverflow;
        private bool hasOverflown = false;

        public void Bump()
        {
            hasOverflown = data == 0xFF;
            if (hasOverflown) OnOverflow?.Invoke();
            data++;
        }
    }
}