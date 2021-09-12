using GB_Emulator.Gameboi.Memory;

namespace GB_Emulator.Gameboi.Graphics
{
    public class Sprite : IMemoryRange
    {
        private readonly IMemory[] data;

        private readonly Byte nr;
        public Byte Nr => nr;

        public Sprite(Byte nr) => (data, this.nr) = (Register.CreateMany(4), nr);

        public byte Y => data[0].Read();
        public byte X => data[1].Read();
        public byte Pattern => data[2].Read();
        public bool Hidden => data[3].Read()[7]; // Other refer to it as "Priority" => 0: display on top, 1: hide under 1,2 and 3 of bg and
        public bool Yflip => data[3].Read()[6];
        public bool Xflip => data[3].Read()[5];
        public bool Palette => data[3].Read()[4];
        public int VramBank => data[3].Read()[3] ? 1 : 0;
        public byte ColorPalette => (byte)(data[3].Read() & 7);

        public int ScreenYstart => Y - 16;
        public int ScreenXstart => X - 8;

        public bool IsWithinScreenWidth() => X > 0 && X < 168;
        public bool IsWithinScreenHeight() => Y > 0 && ScreenYstart < 144;
        public bool IsIntersectWithLine(byte line, bool doubleHeighMode = false)
        {
            int screenYend = ScreenYstart + (doubleHeighMode ? 16 : 8);
            return ScreenYstart <= line && line < screenYend;
        }

        public Address Size => 4;

        public Byte Read(Address address, bool isCpu = false) => data[address].Read();

        public void Write(Address address, Byte value, bool isCpu = false) => data[address].Write(value);

        public void Set(Address address, IMemory replacement) => data[address] = replacement;

    }
}