using System.Collections.Generic;
using System.Linq;
using Gameboi.Memory;

namespace Gameboi.Graphics;

public class OAM : IMemoryRange, ILockable
{
    private bool isLocked = false;

    private readonly Sprite[] sprites = new Sprite[40];

    public OAM()
    {
        for (int i = 0; i < 40; i++)
            sprites[i] = new Sprite(i);
    }

    public bool IsColorMode { get; set; }

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
        result.Sort(SortByMemoryLocation);
        if (!IsColorMode)
            AdjustForEqualX(result);
        return result.Take(10).Reverse().ToArray();
    }

    private int SortByMemoryLocation(Sprite a, Sprite b)
    {
        return a.Nr - b.Nr;
    }

    private void AdjustForEqualX(List<Sprite> sprites)
    {
        for (int i = 0; i < sprites.Count; i++)
        {
            for (int j = 0; j < sprites.Count; j++)
            {
                if (i == j) continue;
                if (System.Math.Abs(sprites[i].X - sprites[j].X) < 8)
                {
                    if (i < j && sprites[i].X > sprites[j].X)
                    {
                        var temp = sprites[i];
                        sprites[i] = sprites[j];
                        sprites[j] = temp;
                    }
                }
            }
        }
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

