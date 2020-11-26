using System.Collections.Generic;

public class OAM : IMemoryRange, ILockable
{
    private bool isLocked = false;

    private Sprite[] sprites = new Sprite[40];

    public OAM()
    {
        for (int i = 0; i < 40; i++)
            sprites[i] = new Sprite(i);
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
        result.Sort((a, b) => b.X - a.X);
        result.Sort((a, b) => b.Nr - a.Nr);
        return result;
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