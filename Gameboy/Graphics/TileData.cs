using GB_Emulator.Memory;
using static GB_Emulator.Statics.TileDataConstants;

namespace GB_Emulator.Graphics
{
    public class TileData : IMemoryRange
    {

        private readonly Tile[] tiles = new Tile[tileCount];

        public Address Size => tileCount * bytesPerTile;

        public TileData()
        {
            for (int i = 0; i < tileCount; i++)
                tiles[i] = new Tile();
        }

        public Byte Read(Address address, bool isCpu = false) => tiles[address / bytesPerTile].Read(address % bytesPerTile, isCpu);

        public void Write(Address address, Byte value, bool isCpu = false) => tiles[address / bytesPerTile].Write(address % bytesPerTile, value, isCpu);

        //dataSelect => false: 0x8800 - 0x97FF | true: 0x8000 - 0x8FFF
        public Tile LoadTilePattern(Byte patternIndex, bool dataSelect)
        {
            int index = patternIndex;

            if (!dataSelect && !patternIndex[7])
                index += startIndexOfSecondTable;

            return tiles[index];
        }

        public void Set(Address address, IMemory replacement) => tiles[address / bytesPerTile].Set(address % bytesPerTile, replacement);

    }
}
