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

        public void Bump()
        {
            if (data == 0xFF)
            {
                OnOverflow?.Invoke();
            }
            data++;
        }
    }
}