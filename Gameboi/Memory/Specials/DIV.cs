using System;
using GB_Emulator.Gameboi.Memory;

namespace GB_Emulator.Gameboi.Memory.Specials
{
    public class DIV : Register
    {
        public DIV(Action onWrite)
        {
            OnWrite = onWrite;
        }
        public override void Write(Byte value)
        {
            OnWrite?.Invoke();
            base.Write(0);
        }
        public virtual void Bump() => data++;

        public Action OnWrite { get; private set; }
    }
}