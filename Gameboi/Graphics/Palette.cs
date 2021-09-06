using GB_Emulator.Gameboi.Memory;

namespace GB_Emulator.Gameboi.Graphics
{
    public class Palette : Register
    {
        public Palette(Byte initialValue) : base(initialValue) { }

        public Byte DecodeColorNumber(byte colorCode) => (~data >> (colorCode * 2)) & 3;
    }

    public class ColorPalette : IMemoryRange
    {
        private Byte palletIndex = new();
        private bool IsAutoIncrementEnabled => (palletIndex & 0x80) > 0;
        private Byte Index => palletIndex & 0x3F;

        public Address Size => 2;

        private readonly Byte[] dataMemory = new Byte[64];

        public (byte, byte, byte) DecodeColorNumber(byte colorCode)
        {
            // if (colorCode > 3) throw new System.ArgumentOutOfRangeException(nameof(colorCode));
            var pallet = colorCode / 7;
            var start = pallet + (2 * (colorCode % 7));
            var lb = dataMemory[start];
            var hb = dataMemory[start + 1];
            var color = (hb << 8) | lb;
            return (
                (byte)((color & 0x1f) << 3),
                (byte)(((color >> 5) & 0x1f) << 3),
                (byte)(((color >> 10) & 0x1f) << 3)
            );
        }

        public Byte Read(Address address, bool isCpu = false)
        {
            if (address > 1) throw new System.ArgumentOutOfRangeException(nameof(address));
            if (address == 0)
            {
                return palletIndex;
            }
            return dataMemory[Index];
        }

        public void Write(Address address, Byte value, bool isCpu = false)
        {
            if (address > 1) throw new System.ArgumentOutOfRangeException(nameof(address));
            if (address == 0)
            {
                palletIndex = value;
                return;
            }
            dataMemory[Index] = value;
            if (IsAutoIncrementEnabled) palletIndex++;
        }

        public void Set(Address address, IMemory replacement)
        {
            throw new System.NotImplementedException();
        }
    }
}