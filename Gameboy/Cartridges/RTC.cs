using System;
using GB_Emulator.Memory;
using Byte = GB_Emulator.Memory.Byte;

namespace GB_Emulator.Cartridges
{
    public class RTC : IMemoryRange
    {

        private int registerPointer = 0;

        private readonly Func<Byte>[] readLambdas = new Func<Byte>[] {
            () => DateTime.Now.Second,
            () => DateTime.Now.Minute,
            () => DateTime.Now.Hour,
            () => (int)DateTime.Now.DayOfWeek,
            () => 0,
        };

        public void SetPointer(int value)
        {
            if (value > 4) throw new ArgumentException(null, nameof(value));
            registerPointer = value;
        }

        public Address Size => 5;

        public Byte Read(Address address, bool isCpu = false)
        {
            return readLambdas[registerPointer]();
        }

        public void Set(Address address, IMemory replacement)
        {
            throw new NotImplementedException();
        }

        public void Write(Address address, Byte value, bool isCpu = false)
        {

        }
    }
}
