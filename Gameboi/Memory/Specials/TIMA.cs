namespace GB_Emulator.Gameboi.Memory.Specials
{
    public class TIMA : DIV
    {
        public TIMA(OverflowHandler handler) : base()
        {
            OnOverflow += handler;
        }

        public delegate void OverflowHandler();

        public OverflowHandler OnOverflow;
        private bool hasOverflown = false;

        public override void Bump()
        {
            if (hasOverflown && OnOverflow != null) OnOverflow();
            else base.Bump();
            hasOverflown = data == 0xFF;
        }
    }
}