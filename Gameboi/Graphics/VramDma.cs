using GB_Emulator.Gameboi.Memory;

namespace GB_Emulator.Gameboi.Graphics
{
    class VramDma : IMemoryRange
    {
        private const int size = 5;
        private Bus bus;
        private readonly Byte[] registers = new Byte[size];

        private Byte HDMA1 => registers[0];
        private Byte HDMA2 => registers[1];
        private Byte HDMA3 => registers[2];
        private Byte HDMA4 => registers[3];
        private Byte HDMA5 => registers[4];

        Address Source => (HDMA1 << 8) | HDMA2;
        Address Destination => (HDMA3 << 8) | HDMA4;

        bool IsHblankMode => HDMA5 > 0x7f;
        int Length => ((HDMA5 & 0x7f) + 1) * 16;

        public Address Size => size;

        public Byte Read(Address address, bool isCpu = false)
        {
            return registers[address];
        }

        public void Set(Address address, IMemory replacement)
        {
            throw new System.NotImplementedException();
        }

        public void Write(Address address, Byte value, bool isCpu = false)
        {
            bool prevIsBlankMode = IsHblankMode;

            if (address == 1 || address == 3)
            {
                value &= 0xf0;
            }

            registers[address] = value;

            if (address == 4)
            {
                if (!IsHblankMode && !prevIsBlankMode)
                {
                    for (int i = 0; i < Length; i++)
                    {
                        bus.Write(Destination + i, bus.Read(Source + i));
                    }
                    bus.UpdateCycles(8 * Length / 16);
                }
            }
        }

        public void TransferIfActive()
        {
            if (IsHblankMode)
            {
                for (int i = 0; i < 16; i++)
                {
                    bus.Write(Destination + i, bus.Read(Source + i));
                }
                var newSource = Source + 16;
                registers[0] = (newSource & 0xff00) >> 8;
                registers[1] = newSource & 0x00ff;

                var newDest = Destination + 16;
                registers[2] = (newDest & 0xff00) >> 8;
                registers[3] = newDest & 0x00ff;

                bus.UpdateCycles(8);
            }
        }

        public void Connect(Bus bus)
        {
            this.bus = bus;
            this.bus.RouteMemory(0xFF51, this);
        }
    }
}