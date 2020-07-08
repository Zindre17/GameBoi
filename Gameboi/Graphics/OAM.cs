using System.Collections.Generic;
using static GeneralMemoryMap;
using static ScreenSizes;

class OAM : IMemoryRange
{
    private Sprite[] sprites = new Sprite[40];

    public OAM()
    {
        for (int i = 0; i < 40; i++)
            sprites[i] = new Sprite();
    }

    public List<Sprite> GetSpritesOnLine(Byte ly, bool isDoubleHeight)
    {
        var result = new List<Sprite>();
        int spriteHeight = isDoubleHeight ? 16 : 8;

        foreach (var sprite in sprites)
        {
            if (!sprite.IsWithinScreenWidth()) continue;
            if (!sprite.IsWithinScreenHeight()) continue;

            if (sprite.IsIntersectWithLine(ly, isDoubleHeight))
                result.Add(sprite);
        }
        return result;
    }

    private Sprite sprite(Address address) => sprites[address / 4];
    private IMemory property(Address address) => sprite(address)[address % 4];

    public IMemory this[Address address] { get => property(address); set { } }

    public Address Size => 40 * 4;

    public Byte Read(Address address) => property(address).Read();

    public void Write(Address address, Byte value) => property(address).Write(value);

}