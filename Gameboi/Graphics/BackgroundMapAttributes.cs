using GB_Emulator.Gameboi.Memory;

namespace GB_Emulator.Gameboi.Graphics
{
    class BackgroundMapAttribute
    {
        private readonly Byte data;
        public BackgroundMapAttribute(Byte data) => this.data = data;

        public Byte PalletNr => data & 7;

        public Byte VramBankNr => (data >> 3) & 1;

        public bool IsHorizontallyFlipped => data[5];
        public bool IsVerticallyFlipped => data[6];
        public bool Priority => data[7]; // true => BG has priority | false => Use OAM priorty bit
    }

    class AttributeMap : IMemoryRange
    {
        public Address Size => throw new System.NotImplementedException();

        public Byte Read(Address address, bool isCpu = false)
        {
            throw new System.NotImplementedException();
        }

        public void Set(Address address, IMemory replacement)
        {
            throw new System.NotImplementedException();
        }

        public void Write(Address address, Byte value, bool isCpu = false)
        {
            throw new System.NotImplementedException();
        }
    }
}