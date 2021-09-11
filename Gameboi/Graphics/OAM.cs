using System.Collections.Generic;
using System.Linq;
using GB_Emulator.Gameboi.Memory;

namespace GB_Emulator.Gameboi.Graphics
{
    public class OAM : IMemoryRange, ILockable
    {
        private bool isLocked = false;

        private readonly Sprite[] sprites = new Sprite[40];

        public OAM()
        {
            for (int i = 0; i < 40; i++)
                sprites[i] = new Sprite(i);
        }

        public Sprite[] GetSpritesOnLine(Byte ly, bool isDoubleHeight)
        {
            var result = new List<Sprite>();

            foreach (var sprite in sprites)
            {
                if (!sprite.IsWithinScreenWidth()) continue;
                if (!sprite.IsWithinScreenHeight()) continue;

                if (sprite.IsIntersectWithLine(ly, isDoubleHeight))
                    result.Add(sprite);
            }
            result.Sort((a, b) =>
            {
                if (a.X == b.X)
                    return b.Nr - a.Nr;
                return b.X - a.X;
            });
            return result.TakeLast(10).Reverse().ToArray();
        }

        public void Set(Address address, IMemory replacement) => sprites[address / 4].Set(address % 4, replacement);

        public Address Size => 40 * 4;

        public Byte Read(Address address, bool isCpu = false)
        {
            if (isCpu && isLocked) return 0xFF;
            return sprites[address / 4].Read(address % 4, isCpu);
        }

        public void Write(Address address, Byte value, bool isCpu = false)
        {
            if (isCpu && isLocked) return;
            sprites[address / 4].Write(address % 4, value, isCpu);
        }

        public void SetLock(bool on) => isLocked = on;

    }
}