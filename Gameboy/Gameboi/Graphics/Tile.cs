using System;
using GB_Emulator.Gameboi.Memory;
using static GB_Emulator.Statics.TileDataConstants;
using Byte = GB_Emulator.Gameboi.Memory.Byte;

namespace GB_Emulator.Gameboi.Graphics
{
    public class Tile : IMemoryRange
    {
        private readonly IMemory[] data = new IMemory[bytesPerTile];

        public Address Size => bytesPerTile;

        public Tile()
        {
            for (int i = 0; i < bytesPerTile; i++)
                data[i] = new Register();
        }

        public byte GetColorCode(Byte x, Byte y)
        {
            if (x > 7)
                throw new ArgumentOutOfRangeException(nameof(x), "x must be between 0 and 7");
            if (y > 7)
                throw new ArgumentOutOfRangeException(nameof(y), "y must be between 0 and 7");

            Byte result = 0;

            Byte rowIndex = y * 2;
            if (data[rowIndex + 1].Read()[7 - x]) result |= 2;
            if (data[rowIndex].Read()[7 - x]) result |= 1;

            return result;
        }

        public Byte Read(Address address, bool isCpu = false) => data[address].Read();

        public void Write(Address address, Byte value, bool isCpu = false) => data[address].Write(value);

        public void Set(Address address, IMemory replacement) => data[address] = replacement;

    }
}
