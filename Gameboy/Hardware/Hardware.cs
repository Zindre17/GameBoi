using GB_Emulator;
using GB_Emulator.Memory;

namespace GB_Emulator.Hardware
{
    public abstract class Hardware
    {
        protected Bus bus;

        public virtual void Connect(Bus bus) => this.bus = bus;

        public virtual Byte Read(Address address) => bus.Read(address);
        public virtual void Write(Address address, Byte value) => bus.Write(address, value);

        public ulong Cycles => bus.Cycles;

    }
}
