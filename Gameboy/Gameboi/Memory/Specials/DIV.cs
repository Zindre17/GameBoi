using System;
using GB_Emulator.Gameboi.Memory;
using GB_Emulator.Statics;

namespace GB_Emulator.Gameboi.Memory.Specials
{
    public class DIV : IMemory
    {
        private Address counter;
        public Action OnWrite { get; private set; }

        public DIV(Action onWrite)
        {
            OnWrite = onWrite;
        }

        public Address Counter => counter;

        public Byte Read()
        {
            return ByteOperations.GetHighByte(counter);
        }

        public void Write(Byte _)
        {
            OnWrite?.Invoke();
            counter = 0;
        }

        public void AddCycles(int cycles)
        {
            var prev = counter;
            counter += cycles;
            if (prev > counter)
                OnWrite?.Invoke();
        }
    }
}