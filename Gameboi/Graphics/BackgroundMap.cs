using Gameboi.Memory;
using static Gameboi.Statics.TileMapConstants;

namespace Gameboi.Graphics
{
    public class BackgroundMap : IMemoryRange
    {
        private readonly IMemory[] tilemaps = new IMemory[tileMapTotalSize];

        public Address Size => tilemaps.Length;

        public BackgroundMap()
        {
            for (int i = 0; i < tileMapTotalSize; i++)
                tilemaps[i] = new Register();
        }

        //mapSelect: false => 0x9800 - 0x9BFF | true => 0x9C00 - 0x9FFF
        public Byte GetTilePatternIndex(Byte x, Byte y, bool mapSelect)
        {
            int index = y * tileMapWidth + x;

            if (mapSelect)
                index += tileMapSize;

            return tilemaps[index].Read();
        }

        public Byte Read(Address address, bool isCpu = false) => tilemaps[address].Read();

        public void Write(Address address, Byte value, bool isCpu = false) => tilemaps[address].Write(value);

        public void Set(Address address, IMemory replacement) => tilemaps[address] = replacement;

    }
}
