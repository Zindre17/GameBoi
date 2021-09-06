using GB_Emulator.Gameboi.Memory;
using static GB_Emulator.Statics.TileMapConstants;

namespace GB_Emulator.Gameboi.Graphics
{
    class BackgroundAttribute : IMemory
    {
        private Byte data;
        public BackgroundAttribute(Byte data) => this.data = data;

        public Byte PalletNr => data & 7;

        public Byte VramBankNr => (data >> 3) & 1;

        public bool IsHorizontallyFlipped => data[5];
        public bool IsVerticallyFlipped => data[6];
        public bool Priority => data[7]; // true => BG has priority | false => Use OAM priorty bit

        public Byte Read()
        {
            return data;
        }

        public void Write(Byte value)
        {
            data = value;
        }
    }

    class BackgroundAttributeMap : IMemoryRange
    {
        private readonly BackgroundAttribute[] backgroundAttributes = new BackgroundAttribute[tileMapTotalSize];
        public Address Size => tileMapTotalSize;

        public BackgroundAttributeMap()
        {
            for (int i = 0; i < tileMapTotalSize; i++)
            {
                backgroundAttributes[i] = new BackgroundAttribute(0);
            }
        }

        public BackgroundAttribute GetTilePatternIndex(Byte x, Byte y, bool mapSelect)
        {
            int index = y * tileMapWidth + x;

            if (mapSelect)
                index += tileMapSize;

            return backgroundAttributes[index];
        }

        public Byte Read(Address address, bool isCpu = false)
        {
            return backgroundAttributes[address].Read();
        }

        public void Set(Address address, IMemory replacement)
        {
            throw new System.NotImplementedException();
        }

        public void Write(Address address, Byte value, bool isCpu = false)
        {
            backgroundAttributes[address].Write(value);
        }
    }
}