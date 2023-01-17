using GB_Emulator.Memory;

namespace GB_Emulator.Graphics
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

        Address Source => (HDMA1 << 8) | HDMA2 & 0xF0;
        Address Destination => ((HDMA3 | 0x80) << 8) | HDMA4 & 0xF0;

        bool IsHblankMode => HDMA5 > 0x7f;
        int Length => (Byte)((HDMA5 & 0x7f) + 1) * 16;

        public Address Size => size;

        public Byte Read(Address address, bool isCpu = false)
        {
            return registers[address];
        }

        public void Set(Address address, IMemory replacement)
        {
            throw new System.NotImplementedException();
        }

        private bool isActive = false;
        private int totalLength;
        private int currentLength;
        private Address sourceStart;
        private Address destinationStart;

        public void Write(Address address, Byte value, bool isCpu = false)
        {
            registers[address] = value;

            if (address == 4)
            {
                if (isActive && !IsHblankMode)
                {
                    isActive = false;
                    registers[4] = registers[4] | 0x80;
                    return;
                }

                if (IsHblankMode)
                {
                    isActive = true;
                    totalLength = Length;
                    currentLength = 0;
                    sourceStart = Source;
                    destinationStart = Destination;
                }
                else
                {
                    isActive = false;
                    currentLength = 0;
                    while (TransferBlock(currentLength, Length))
                    {
                        currentLength += 16;
                        registers[4]--;
                    }
                    registers[4] = 0xFF;
                }
            }
        }

        private bool TransferBlock(int start, int max)
        {
            for (int i = start; i < start + 16; i++)
            {
                bus.Write(destinationStart + i, bus.Read(sourceStart + i), true);
            }
            registers[4]--;
            bus.UpdateCycles(8, bus.GetCpuSpeed());
            return start < max;
        }

        public void TransferIfActive()
        {
            if (isActive)
            {
                isActive = TransferBlock(currentLength, totalLength);
                currentLength += 16;
                if (!isActive)
                {
                    registers[4] = 0xFF;
                }
            }
        }

        public void Connect(Bus bus)
        {
            this.bus = bus;
            this.bus.RouteMemory(0xFF51, this);
        }
    }
}
